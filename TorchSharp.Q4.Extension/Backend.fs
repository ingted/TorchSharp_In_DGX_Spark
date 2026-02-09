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

  type private StubPreparedWeight(format: QuantFormat, device: string, debugName: string) =
    interface IQ4PreparedWeight with
      member _.Format = format
      member _.Device = device
      member _.DebugName = debugName
      member _.Dispose() = ()

  type private StubBackend(name: string, supportsFormat: QuantFormat -> bool) =
    interface IQ4Backend with
      member _.Name = name

      member _.Supports(schema: Q4Schema, _config: Q4SessionConfig) =
        supportsFormat schema.Format

      member _.PrepareWeight(schema: Q4Schema, _tensors: Q4TensorBundle, device: string) =
        if not (supportsFormat schema.Format) then
          raise (InvalidOperationException(sprintf "Backend '%s' does not support format %A." name schema.Format))
        new StubPreparedWeight(schema.Format, device, sprintf "%s:%s" name schema.WeightKey) :> IQ4PreparedWeight

      member _.Linear(_input: TorchSharp.torch.Tensor, _prepared: IQ4PreparedWeight, _outDtype: TorchSharp.torch.ScalarType) =
        raise (NotImplementedException(sprintf "Backend '%s' linear kernel is not implemented yet." name))

  let normalizeName (name: string) =
    name.Trim().ToLowerInvariant()

  let supportsOnly (format: QuantFormat) =
    fun inputFormat -> inputFormat = format

  let supportsAll (_: QuantFormat) = true

  let kernelNameByFormat (format: QuantFormat) =
    match format with
    | NF4 -> Nf4KernelName
    | NVFP4 -> Nvfp4KernelName

  let isKernelName (name: string) =
    name = Nf4KernelName || name = Nvfp4KernelName

  let backendFromName (schema: Q4Schema) (name: string) : Result<IQ4Backend, string> =
    match name with
    | n when n = Nf4KernelName ->
      if schema.Format <> NF4 then
        Error(sprintf "Backend '%s' does not match schema format %A." n schema.Format)
      elif NativeInterop.isNf4Available() then
        Ok (StubBackend(n, supportsOnly NF4) :> IQ4Backend)
      else
        Error(sprintf "Backend '%s' requested but NF4 native library is unavailable." n)

    | n when n = Nvfp4KernelName ->
      if schema.Format <> NVFP4 then
        Error(sprintf "Backend '%s' does not match schema format %A." n schema.Format)
      elif NativeInterop.isNvfp4Available() then
        Ok (StubBackend(n, supportsOnly NVFP4) :> IQ4Backend)
      else
        Error(sprintf "Backend '%s' requested but NVFP4 native library is unavailable." n)

    | n when n = DequantFallbackName ->
      Ok (StubBackend(n, supportsAll) :> IQ4Backend)

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
          | Ok backend -> created <- Some backend
          | Error err -> failures.Add err

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
