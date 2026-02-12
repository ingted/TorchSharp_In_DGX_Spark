namespace TorchSharp.Q4.Extension

open System
open System.IO
open System.Reflection
open System.Runtime.InteropServices
open TorchSharp

module NativeInteropImpl =
  type NativeLoadResultInternal =
    {
      LibraryPath: string
      Loaded: bool
      Error: string option
    }

  let mutable nvfp4OverridePath : string option = None
  let mutable nf4OverridePath : string option = None

  [<Literal>]
  let LibTorchSharp = "LibTorchSharp"

  [<Literal>]
  let Nvfp4LibAbsolute = "/workspace/nvfp4_native/libNVFP4.so"

  let tensorCtor : ConstructorInfo option Lazy =
    lazy
      let t = typeof<torch.Tensor>
      t.GetConstructor(
        BindingFlags.NonPublic ||| BindingFlags.Instance,
        null,
        [| typeof<IntPtr>; typeof<bool> |],
        null
      )
      |> Option.ofObj

  [<DllImport(Nvfp4LibAbsolute)>]
  extern void NVFP4_quantize(nativeint input, nativeint& qdata, nativeint& scale)

  [<DllImport(Nvfp4LibAbsolute)>]
  extern nativeint NVFP4_scaled_mm(nativeint mat1, nativeint mat2, nativeint scaleA, nativeint scaleB, sbyte outDtype)

  [<DllImport(Nvfp4LibAbsolute)>]
  extern void NVFP4_empty_cache()

  [<DllImport(Nvfp4LibAbsolute)>]
  extern int32 NVFP4_can_use_managed()

  [<DllImport(Nvfp4LibAbsolute)>]
  extern int32 NVFP4_is_managed_tensor(nativeint tensor)

  [<DllImport(Nvfp4LibAbsolute)>]
  extern nativeint NVFP4_to_managed(nativeint input, int32 prefetchDevice, int32 readMostly)

  [<DllImport(Nvfp4LibAbsolute)>]
  extern void NVFP4_managed_prefetch(nativeint tensor, int32 deviceIndex)

  let emptyNvfp4CacheRaw () =
    NVFP4_empty_cache()

  let canUseManagedRaw () =
    NVFP4_can_use_managed() <> 0

  let isManagedTensorRaw (tensor: torch.Tensor) =
    if isNull tensor then
      false
    else
      NVFP4_is_managed_tensor(tensor.Handle) <> 0

  let prefetchManagedRaw (tensor: torch.Tensor) (deviceIndex: int) =
    NVFP4_managed_prefetch(tensor.Handle, int32 deviceIndex)

  let configurePaths (nvfp4Path: string option) (nf4Path: string option) =
    nvfp4OverridePath <- nvfp4Path
    nf4OverridePath <- nf4Path

  let candidatePaths (overridePath: string option) (envVar: string) (defaults: string list) =
    let envPath = Environment.GetEnvironmentVariable(envVar) |> Option.ofObj
    match overridePath, envPath with
    | Some p, _ -> [ p ]
    | None, Some p when not (String.IsNullOrWhiteSpace(p)) -> p :: defaults
    | None, _ -> defaults

  let tryLoadOne (candidate: string) : Result<unit, string> =
    try
      let mutable handle = IntPtr.Zero
      if NativeLibrary.TryLoad(candidate, &handle) then
        NativeLibrary.Free(handle)
        Ok ()
      else
        Error "dynamic loader returned false"
    with ex ->
      Error ex.Message

  let loadFirst (candidates: string list) : NativeLoadResultInternal =
    let mutable firstError : string option = None
    let rec loop (items: string list) =
      match items with
      | [] ->
        {
          LibraryPath = candidates |> List.tryHead |> Option.defaultValue ""
          Loaded = false
          Error = firstError
        }
      | path :: rest ->
        match tryLoadOne path with
        | Ok () ->
          {
            LibraryPath = path
            Loaded = true
            Error = None
          }
        | Error err ->
          if firstError.IsNone then
            firstError <- Some (sprintf "%s -> %s" path err)
          loop rest
    loop candidates

  let loadNvfp4 () =
    let candidates =
      candidatePaths
        nvfp4OverridePath
        "NVFP4_LIB_PATH"
        [ "/workspace/nvfp4_native/libNVFP4.so"; "libNVFP4.so" ]
    loadFirst candidates

  let loadNf4 () =
    let candidates = candidatePaths nf4OverridePath "NF4_LIB_PATH" [ "libbitsandbytes.so"; "libNF4.so" ]
    loadFirst candidates

  let tryGetExportFromCandidates (candidates: string list) (symbolName: string) : bool =
    candidates
    |> List.exists (fun candidate ->
      try
        let mutable handle = IntPtr.Zero
        if not (NativeLibrary.TryLoad(candidate, &handle)) then
          false
        else
          let mutable symbol = IntPtr.Zero
          let ok = NativeLibrary.TryGetExport(handle, symbolName, &symbol)
          NativeLibrary.Free(handle)
          ok
      with _ ->
        false)

  let nvfp4Candidates () =
    candidatePaths
      nvfp4OverridePath
      "NVFP4_LIB_PATH"
      [ "/workspace/nvfp4_native/libNVFP4.so"; "libNVFP4.so" ]

  let libTorchSharpCandidates () =
    let baseDir = AppContext.BaseDirectory
    let defaultCandidates =
      [
        LibTorchSharp
        "libLibTorchSharp.so"
        Path.Combine(baseDir, "libLibTorchSharp.so")
      ]
    let nugetCandidate =
      let home = Environment.GetEnvironmentVariable("HOME")
      if String.IsNullOrWhiteSpace(home) then
        None
      else
        let p = Path.Combine(home, ".nuget/packages/fakka.torchsharp.dgx/26.1.0-py3.7/runtimes/linux-arm64/native/libLibTorchSharp.so")
        if File.Exists(p) then Some p else None
    match nugetCandidate with
    | Some p -> p :: defaultCandidates
    | None -> defaultCandidates

  let tryGetExport (symbolName: string) : bool =
    tryGetExportFromCandidates (libTorchSharpCandidates ()) symbolName

  let fromTensorPointer (handle: nativeint) : torch.Tensor =
    if handle = 0n then
      null
    else
      match tensorCtor.Value with
      | None ->
        raise (InvalidOperationException("Unable to create Tensor from native pointer: non-public ctor not found."))
      | Some ctor ->
        ctor.Invoke([| box (IntPtr(int64 handle)); box true |]) :?> torch.Tensor

  let toManagedRaw (input: torch.Tensor) (prefetchDevice: int option) (readMostly: bool) =
    let prefetch = defaultArg prefetchDevice -1
    let outHandle = NVFP4_to_managed(input.Handle, int32 prefetch, if readMostly then 1 else 0)
    if outHandle = 0n then
      torch.CheckForErrors()
      raise (InvalidOperationException("NVFP4_to_managed returned null tensor pointer."))
    torch.CheckForErrors()
    let output = fromTensorPointer outHandle
    if isNull output then
      raise (InvalidOperationException("NVFP4_to_managed returned invalid tensor pointer."))
    output

  let fp4QuantizeRaw (input: torch.Tensor) : torch.Tensor * torch.Tensor =
    let mutable qdataHandle = 0n
    let mutable scaleHandle = 0n
    NVFP4_quantize(input.Handle, &qdataHandle, &scaleHandle)
    torch.CheckForErrors()

    let qdata = fromTensorPointer qdataHandle
    let scale = fromTensorPointer scaleHandle

    if isNull qdata || isNull scale then
      raise (InvalidOperationException("NVFP4_quantize returned null tensor pointer."))

    qdata, scale

  let scaledMmRaw
    (mat1: torch.Tensor)
    (mat2: torch.Tensor)
    (scaleA: torch.Tensor)
    (scaleB: torch.Tensor)
    (outDtype: torch.ScalarType)
    : torch.Tensor
    =
    let outHandle = NVFP4_scaled_mm(mat1.Handle, mat2.Handle, scaleA.Handle, scaleB.Handle, sbyte outDtype)

    if outHandle = 0n then
      torch.CheckForErrors()
      raise (InvalidOperationException("NVFP4_scaled_mm returned null tensor pointer."))

    torch.CheckForErrors()

    let output = fromTensorPointer outHandle
    if isNull output then
      raise (InvalidOperationException("NVFP4_scaled_mm returned invalid tensor pointer."))

    output

module NativeInterop =
  type NativeLoadResult =
    {
      LibraryPath: string
      Loaded: bool
      Error: string option
    }

  let toPublicResult (r: NativeInteropImpl.NativeLoadResultInternal) : NativeLoadResult =
    {
      LibraryPath = r.LibraryPath
      Loaded = r.Loaded
      Error = r.Error
    }

  let configure (nvfp4Path: string option, nf4Path: string option) : unit =
    NativeInteropImpl.configurePaths nvfp4Path nf4Path

  let loadNvfp4 () : NativeLoadResult =
    NativeInteropImpl.loadNvfp4 () |> toPublicResult

  let loadNf4 () : NativeLoadResult =
    NativeInteropImpl.loadNf4 () |> toPublicResult

  let isNvfp4Available () : bool =
    (loadNvfp4 ()).Loaded

  let isNf4Available () : bool =
    (loadNf4 ()).Loaded

  let hasLibTorchFp4Quantize () : bool =
    NativeInteropImpl.tryGetExportFromCandidates (NativeInteropImpl.nvfp4Candidates ()) "NVFP4_quantize"

  let hasLibTorchScaledMm () : bool =
    NativeInteropImpl.tryGetExportFromCandidates (NativeInteropImpl.nvfp4Candidates ()) "NVFP4_scaled_mm"

  let hasNvfp4EmptyCache () : bool =
    NativeInteropImpl.tryGetExportFromCandidates (NativeInteropImpl.nvfp4Candidates ()) "NVFP4_empty_cache"

  let hasManagedSupport () : bool =
    NativeInteropImpl.tryGetExportFromCandidates (NativeInteropImpl.nvfp4Candidates ()) "NVFP4_to_managed"
    && NativeInteropImpl.tryGetExportFromCandidates (NativeInteropImpl.nvfp4Candidates ()) "NVFP4_is_managed_tensor"
    && NativeInteropImpl.tryGetExportFromCandidates (NativeInteropImpl.nvfp4Candidates ()) "NVFP4_can_use_managed"

  let fp4Quantize (input: torch.Tensor) : torch.Tensor * torch.Tensor =
    if not (hasLibTorchFp4Quantize()) then
      raise (InvalidOperationException("NVFP4 export NVFP4_quantize is unavailable."))
    NativeInteropImpl.fp4QuantizeRaw input

  let scaledMmFp4
    (mat1: torch.Tensor)
    (mat2: torch.Tensor)
    (scaleA: torch.Tensor)
    (scaleB: torch.Tensor)
    (outDtype: torch.ScalarType)
    : torch.Tensor
    =
    if not (hasLibTorchScaledMm()) then
      raise (InvalidOperationException("NVFP4 export NVFP4_scaled_mm is unavailable."))
    NativeInteropImpl.scaledMmRaw mat1 mat2 scaleA scaleB outDtype

  let tryEmptyNvfp4Cache () : bool =
    if not (hasNvfp4EmptyCache()) then
      false
    else
      NativeInteropImpl.emptyNvfp4CacheRaw()
      true

  let canUseManaged () : bool =
    if not (hasManagedSupport()) then
      false
    else
      NativeInteropImpl.canUseManagedRaw()

  let isManagedTensor (tensor: torch.Tensor) : bool =
    if not (hasManagedSupport()) then
      false
    else
      NativeInteropImpl.isManagedTensorRaw tensor

  let toManagedTensor (tensor: torch.Tensor) (prefetchDevice: int option) (readMostly: bool) : torch.Tensor =
    if not (hasManagedSupport()) then
      raise (InvalidOperationException("NVFP4 managed tensor exports are unavailable."))
    NativeInteropImpl.toManagedRaw tensor prefetchDevice readMostly

  let prefetchManagedTensor (tensor: torch.Tensor) (deviceIndex: int) : unit =
    if hasManagedSupport() then
      NativeInteropImpl.prefetchManagedRaw tensor deviceIndex
