namespace TorchSharp.Q4.Extension

open System
open System.IO
open TorchSharp

module UnifiedMemoryImpl =
  let envFlag (name: string) =
    Environment.GetEnvironmentVariable(name)
    |> Option.ofObj
    |> Option.defaultValue ""
    |> fun s -> s.Trim()
    |> fun s -> s = "1" || s.Equals("true", StringComparison.OrdinalIgnoreCase)

  let hasUvmDriverNode () =
    File.Exists("/proc/driver/nvidia-uvm/version") || File.Exists("/dev/nvidia-uvm")

  let requireSupported (policy: UnifiedMemoryPolicy) =
    if policy = RequireUnified && not (hasUvmDriverNode() && torch.cuda_is_available()) then
      raise (InvalidOperationException("Unified Memory required by policy but UVM/CUDA capability is not available."))

module UnifiedMemory =
  let isSupported () : bool =
    if UnifiedMemoryImpl.envFlag "TS_Q4_DISABLE_UM" then
      false
    else
      torch.cuda_is_available() && UnifiedMemoryImpl.hasUvmDriverNode()

  let applyPolicy (policy: UnifiedMemoryPolicy) (tensor: TorchSharp.torch.Tensor) : TorchSharp.torch.Tensor =
    match policy with
    | Disabled -> tensor
    | PreferUnified ->
      let _ = isSupported()
      tensor
    | RequireUnified ->
      UnifiedMemoryImpl.requireSupported policy
      tensor

  let applyMutablePolicy (policy: UnifiedMemoryPolicy) (tensor: TorchSharp.torch.Tensor) : TorchSharp.torch.Tensor =
    let baseTensor = applyPolicy policy tensor
    match policy with
    | Disabled -> baseTensor
    | PreferUnified
    | RequireUnified ->
      // For actor-shared mutable paths, return a detached clone to avoid accidental aliasing.
      baseTensor.clone().detach()
