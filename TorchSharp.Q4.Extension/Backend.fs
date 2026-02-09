namespace TorchSharp.Q4.Extension

open System
open TorchSharp

module private BackendImpl =
  [<Literal>]
  let Nf4KernelName = "nf4-kernel"

  [<Literal>]
  let Nvfp4KernelName = "nvfp4-kernel"

  [<Literal>]
  let DequantFallbackName = "dequant-matmul"

  let isFloatingDtype (dtype: TorchSharp.torch.ScalarType) =
    dtype = torch.float16
    || dtype = torch.float32
    || dtype = torch.float64
    || dtype = torch.bfloat16

  let isIntegralDtype (dtype: TorchSharp.torch.ScalarType) =
    dtype = torch.uint8
    || dtype = torch.int8
    || dtype = torch.int16
    || dtype = torch.int32
    || dtype = torch.int64

  let sameDevice (a: TorchSharp.torch.Tensor) (b: TorchSharp.torch.Tensor) =
    a.device_type = b.device_type && a.device_index = b.device_index

  let toFloat32On (target: TorchSharp.torch.Tensor) (tensor: TorchSharp.torch.Tensor) =
    let asFloat = if tensor.dtype = torch.float32 then tensor else tensor.to_type(torch.float32)
    if sameDevice asFloat target then asFloat else asFloat.``to``(target.device)

  let tensorElementCount (tensor: TorchSharp.torch.Tensor) =
    tensor.shape |> Array.fold (fun acc d -> acc * d) 1L

  let ensureMatrix (name: string) (tensor: TorchSharp.torch.Tensor) =
    if tensor.shape.Length <> 2 then
      raise (InvalidOperationException(sprintf "%s must be rank-2, got rank=%d." name tensor.shape.Length))
    tensor

  let decodePacked4BitWithCodebook
    (packed2d: TorchSharp.torch.Tensor)
    (codebook: TorchSharp.torch.Tensor)
    : TorchSharp.torch.Tensor
    =
    let packed = ensureMatrix "packed weight" packed2d
    let packedU8 = packed.to_type(torch.uint8)
    let lowMask = torch.tensor(15uy, dtype = torch.uint8, device = packed.device)
    let shift4 = torch.tensor(4uy, dtype = torch.uint8, device = packed.device)

    let low = torch.bitwise_and(packedU8, lowMask)
    let high = torch.bitwise_right_shift(packedU8, shift4)

    let lowIdx = low.to_type(torch.int64).reshape(-1L)
    let highIdx = high.to_type(torch.int64).reshape(-1L)

    let qmap = toFloat32On packed (codebook.reshape(-1L))
    if qmap.shape.[0] < 16L then
      raise (InvalidOperationException("Codebook length must be >= 16 for packed 4-bit decode."))

    let qmap16 = if qmap.shape.[0] = 16L then qmap else qmap.narrow(0L, 0L, 16L)
    let lowVals = torch.index_select(qmap16, 0L, lowIdx)
    let highVals = torch.index_select(qmap16, 0L, highIdx)
    let paired = torch.stack([| lowVals; highVals |], dim = 1L)
    let flat = paired.reshape(-1L)

    let outFeatures = packed.shape.[0]
    let inFeatures = packed.shape.[1] * 2L
    flat.reshape([| outFeatures; inFeatures |])

  let applyBlockScale
    (values: TorchSharp.torch.Tensor)
    (scales: TorchSharp.torch.Tensor option)
    : TorchSharp.torch.Tensor
    =
    match scales with
    | None -> values
    | Some scaleTensor ->
      let vflat = values.reshape(-1L)
      let sflat = toFloat32On vflat (scaleTensor.reshape(-1L))
      let sCount = sflat.shape.[0]
      if sCount <= 0L then
        values
      else
        let vCount = vflat.shape.[0]
        let expanded =
          if sCount = vCount then
            sflat
          else
            let repeats = int64 (Math.Ceiling(float vCount / float sCount))
            let tiled = sflat.repeat([| repeats |])
            tiled.narrow(0L, 0L, vCount)
        (vflat * expanded).reshape(values.shape)

  let nvfp4Codebook (device: TorchSharp.torch.Device) =
    let values =
      [|
        0.0f; 0.5f; 1.0f; 1.5f
        2.0f; 3.0f; 4.0f; 6.0f
        -0.0f; -0.5f; -1.0f; -1.5f
        -2.0f; -3.0f; -4.0f; -6.0f
      |]
    torch.tensor(values, dtype = torch.float32).``to``(device)

  let materializeNf4Weight (bundle: Q4TensorBundle) =
    let weight = ensureMatrix "NF4 weight" bundle.Weight
    if isFloatingDtype weight.dtype then
      weight.to_type(torch.float32)
    elif isIntegralDtype weight.dtype then
      match bundle.QuantMap, bundle.Absmax with
      | Some quantMap, Some absmax ->
        let decoded = decodePacked4BitWithCodebook weight quantMap
        applyBlockScale decoded (Some absmax)
      | _ ->
        raise (InvalidOperationException("NF4 decode requires both QuantMap and Absmax tensors."))
    else
      raise (InvalidOperationException(sprintf "Unsupported NF4 weight dtype: %A" weight.dtype))

  let materializeNvfp4Weight (bundle: Q4TensorBundle) =
    let qdata = ensureMatrix "NVFP4 qdata" bundle.Weight
    if isFloatingDtype qdata.dtype then
      qdata.to_type(torch.float32)
    elif isIntegralDtype qdata.dtype then
      match bundle.Scale with
      | Some scale ->
        let codebook = nvfp4Codebook qdata.device
        let decoded = decodePacked4BitWithCodebook qdata codebook
        applyBlockScale decoded (Some scale)
      | None ->
        raise (InvalidOperationException("NVFP4 decode requires a Scale tensor."))
    else
      raise (InvalidOperationException(sprintf "Unsupported NVFP4 qdata dtype: %A" qdata.dtype))

  let materializeWeight (schema: Q4Schema) (bundle: Q4TensorBundle) =
    match schema.Format with
    | NF4 -> materializeNf4Weight bundle
    | NVFP4 -> materializeNvfp4Weight bundle

  type private PreparedWeight(format: QuantFormat, device: string, debugName: string, denseWeight: TorchSharp.torch.Tensor) =
    let mutable isDisposed = false

    member _.DenseWeight =
      if isDisposed then
        raise (ObjectDisposedException(debugName))
      denseWeight

    interface IQ4PreparedWeight with
      member _.Format = format
      member _.Device = device
      member _.DebugName = debugName

      member _.Dispose() =
        if not isDisposed then
          isDisposed <- true
          denseWeight.Dispose()

  let ensureInputFeatureMatch (input: TorchSharp.torch.Tensor) (weight: TorchSharp.torch.Tensor) =
    if input.shape.Length < 1 then
      raise (InvalidOperationException("Input tensor rank must be >= 1."))
    if weight.shape.Length <> 2 then
      raise (InvalidOperationException("Prepared dense weight must be rank-2."))
    let inFeatures = input.shape.[input.shape.Length - 1]
    if inFeatures <> weight.shape.[1] then
      raise (
        InvalidOperationException(
          sprintf "Input feature size mismatch: input=%d, weight.in=%d." inFeatures weight.shape.[1]
        )
      )

  let ensureComputeDtype (tensor: TorchSharp.torch.Tensor) =
    if isFloatingDtype tensor.dtype then tensor.dtype else torch.float32

  let private runLinearFallback
    (input: TorchSharp.torch.Tensor)
    (prepared: PreparedWeight)
    (outDtype: TorchSharp.torch.ScalarType)
    =
    let dense = prepared.DenseWeight
    ensureInputFeatureMatch input dense

    let inputOnWeightDevice =
      if sameDevice input dense then input else input.``to``(dense.device)

    let computeDtype = ensureComputeDtype inputOnWeightDevice
    let inputForCompute =
      if inputOnWeightDevice.dtype = computeDtype then
        inputOnWeightDevice
      else
        inputOnWeightDevice.to_type(computeDtype)

    let weightForCompute =
      if dense.dtype = computeDtype then
        dense
      else
        dense.to_type(computeDtype)

    let output = torch.nn.functional.linear(inputForCompute, weightForCompute)
    if output.dtype = outDtype then output else output.to_type(outDtype)

  type private RuntimeBackend
    (
      name: string,
      supportsFormat: QuantFormat -> bool,
      requireNative: unit -> bool,
      nativeRequirementLabel: string option
    ) =
    interface IQ4Backend with
      member _.Name = name

      member _.Supports(schema: Q4Schema, _config: Q4SessionConfig) =
        supportsFormat schema.Format

      member _.PrepareWeight(schema: Q4Schema, tensors: Q4TensorBundle, device: string) =
        if not (supportsFormat schema.Format) then
          raise (InvalidOperationException(sprintf "Backend '%s' does not support format %A." name schema.Format))

        match nativeRequirementLabel with
        | Some label when not (requireNative()) ->
          raise (InvalidOperationException(sprintf "Backend '%s' requested but native %s is unavailable." name label))
        | _ -> ()

        let dense = materializeWeight schema tensors
        let denseOnTarget =
          if dense.device.ToString() = device then dense else dense.``to``(device)
        let detached = denseOnTarget.detach().clone()
        new PreparedWeight(schema.Format, device, sprintf "%s:%s" name schema.WeightKey, detached) :> IQ4PreparedWeight

      member _.Linear(input: TorchSharp.torch.Tensor, prepared: IQ4PreparedWeight, outDtype: TorchSharp.torch.ScalarType) =
        match prepared with
        | :? PreparedWeight as p ->
          runLinearFallback input p outDtype
        | _ ->
          raise (InvalidOperationException("Prepared weight type mismatch for backend implementation."))

  let normalizeName (name: string) = name.Trim().ToLowerInvariant()

  let supportsOnly (format: QuantFormat) = fun x -> x = format
  let supportsAll (_: QuantFormat) = true

  let kernelNameByFormat (format: QuantFormat) =
    match format with
    | NF4 -> Nf4KernelName
    | NVFP4 -> Nvfp4KernelName

  let isKernelName (name: string) = name = Nf4KernelName || name = Nvfp4KernelName

  let backendFromName (schema: Q4Schema) (name: string) : Result<IQ4Backend, string> =
    match name with
    | n when n = Nf4KernelName ->
      if schema.Format <> NF4 then
        Error(sprintf "Backend '%s' does not match schema format %A." n schema.Format)
      else
        Ok (new RuntimeBackend(n, supportsOnly NF4, NativeInterop.isNf4Available, Some "NF4") :> IQ4Backend)

    | n when n = Nvfp4KernelName ->
      if schema.Format <> NVFP4 then
        Error(sprintf "Backend '%s' does not match schema format %A." n schema.Format)
      else
        Ok (new RuntimeBackend(n, supportsOnly NVFP4, NativeInterop.isNvfp4Available, Some "NVFP4") :> IQ4Backend)

    | n when n = DequantFallbackName ->
      Ok (new RuntimeBackend(n, supportsAll, (fun () -> true), None) :> IQ4Backend)

    | unknown ->
      Error(sprintf "Unknown backend override '%s'." unknown)

  let candidates (schema: Q4Schema) (config: Q4SessionConfig) : Result<string list, string> =
    let requested = config.BackendOverride |> Option.map normalizeName
    match config.ComputePath, requested with
    | Q4ComputePath.DequantMatmulOnly, Some name when name <> DequantFallbackName ->
      Error(
        sprintf
          "ComputePath is DequantMatmulOnly but backend override '%s' is not '%s'."
          name
          DequantFallbackName
      )

    | Q4ComputePath.DequantMatmulOnly, _ ->
      Ok [ DequantFallbackName ]

    | Q4ComputePath.KernelOnly, Some name when not (isKernelName name) ->
      Error(sprintf "ComputePath is KernelOnly but backend override '%s' is not a kernel backend." name)

    | Q4ComputePath.KernelOnly, Some name ->
      Ok [ name ]

    | Q4ComputePath.KernelOnly, None ->
      Ok [ kernelNameByFormat schema.Format ]

    | (Q4ComputePath.KernelOrFallback | Q4ComputePath.Auto), Some name ->
      Ok [ name ]

    | (Q4ComputePath.KernelOrFallback | Q4ComputePath.Auto), None ->
      Ok [ kernelNameByFormat schema.Format; DequantFallbackName ]

module Backend =
  let tryCreate (schema: Q4Schema) (config: Q4SessionConfig) : Result<IQ4Backend, string> =
    match BackendImpl.candidates schema config with
    | Error err -> Error err
    | Ok candidateNames ->
      let mutable created : IQ4Backend option = None
      let failures = ResizeArray<string>()

      for name in candidateNames do
        if created.IsNone then
          match BackendImpl.backendFromName schema name with
          | Ok backend ->
            if backend.Supports(schema, config) then
              created <- Some backend
            else
              failures.Add(sprintf "Backend '%s' does not support schema/config combination." backend.Name)
          | Error err ->
            failures.Add err

      match created with
      | Some backend -> Ok backend
      | None ->
        let details =
          if failures.Count = 0 then
            "No backend candidate was produced by dispatcher."
          else
            String.concat " | " (failures |> Seq.toList)
        Error(sprintf "Unable to create backend for format %A. %s" schema.Format details)

  let create (schema: Q4Schema) (config: Q4SessionConfig) : IQ4Backend =
    match tryCreate schema config with
    | Ok backend -> backend
    | Error err -> raise (InvalidOperationException(err))

  let listAvailable () : string list =
    let names = ResizeArray<string>()
    if NativeInterop.isNf4Available() then
      names.Add(BackendImpl.Nf4KernelName)
    if NativeInterop.isNvfp4Available() then
      names.Add(BackendImpl.Nvfp4KernelName)
    names.Add(BackendImpl.DequantFallbackName)
    names |> Seq.distinct |> Seq.toList
