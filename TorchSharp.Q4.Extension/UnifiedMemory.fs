namespace TorchSharp.Q4.Extension

open System
open System.IO
open TorchSharp

module UnifiedMemoryImpl =
  let envValue (name: string) =
    Environment.GetEnvironmentVariable(name)
    |> Option.ofObj
    |> Option.defaultValue ""
    |> fun s -> s.Trim().ToLowerInvariant()

  let disableFlagEnabled () =
    match envValue "TS_Q4_DISABLE_UM" with
    | "1"
    | "true"
    | "yes" -> true
    | _ -> false

  let disableFlagForcesEnable () =
    match envValue "TS_Q4_DISABLE_UM" with
    | "0"
    | "false"
    | "no" -> true
    | _ -> false

  let hasUvmDriverNode () =
    File.Exists("/proc/driver/nvidia-uvm/version") || File.Exists("/dev/nvidia-uvm")

  let requireSupported (policy: UnifiedMemoryPolicy) =
    if policy = RequireUnified && not (hasUvmDriverNode() && torch.cuda_is_available()) then
      raise (InvalidOperationException("Unified Memory required by policy but UVM/CUDA capability is not available."))

module UnifiedMemory =
  let isSupported () : bool =
    if UnifiedMemoryImpl.disableFlagEnabled() then
      false
    else
      torch.cuda_is_available()
      && UnifiedMemoryImpl.hasUvmDriverNode()
      && NativeInterop.hasManagedSupport()
      && NativeInterop.canUseManaged()

  let isPolicyEnabledByEnv () : bool =
    UnifiedMemoryImpl.disableFlagForcesEnable() || not (UnifiedMemoryImpl.disableFlagEnabled())

  let isManagedTensor (tensor: TorchSharp.torch.Tensor) : bool =
    NativeInterop.hasManagedSupport() && NativeInterop.isManagedTensor tensor

  let private inferPrefetchDevice (tensor: TorchSharp.torch.Tensor) =
    if tensor.device_type = DeviceType.CUDA then
      Some tensor.device_index
    else
      if torch.cuda_is_available() then Some 0 else None

  let tryPromoteToManaged (tensor: TorchSharp.torch.Tensor) (readMostly: bool) (prefetchDevice: int option) : TorchSharp.torch.Tensor =
    if isNull tensor then
      tensor
    elif not (isSupported() && isPolicyEnabledByEnv()) then
      tensor
    elif isManagedTensor tensor then
      tensor
    else
      let targetPrefetch = defaultArg prefetchDevice (inferPrefetchDevice tensor |> Option.defaultValue -1) |> Some
      NativeInterop.toManagedTensor tensor targetPrefetch readMostly

  let applyPolicy (policy: UnifiedMemoryPolicy) (tensor: TorchSharp.torch.Tensor) : TorchSharp.torch.Tensor =
    match policy with
    | Disabled -> tensor
    | PreferUnified ->
      tryPromoteToManaged tensor false None
    | RequireUnified ->
      UnifiedMemoryImpl.requireSupported policy
      let managed = tryPromoteToManaged tensor false None
      if isManagedTensor managed then
        managed
      else
        raise (InvalidOperationException("Unified Memory required but managed conversion was not applied."))

  let applyMutablePolicy (policy: UnifiedMemoryPolicy) (tensor: TorchSharp.torch.Tensor) : TorchSharp.torch.Tensor =
    match policy with
    | Disabled -> tensor
    | PreferUnified
    | RequireUnified ->
      // Mutable paths should not alias caller tensors; clone+detach first, then promote.
      let cloned = tensor.clone().detach()
      let promoted = applyPolicy policy cloned
      if not (Object.ReferenceEquals(cloned, promoted)) then
        cloned.Dispose()
      promoted
