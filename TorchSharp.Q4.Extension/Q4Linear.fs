namespace TorchSharp.Q4.Extension

open System
open TorchSharp

type Q4Linear(config: Q4SessionConfig, schema: Q4Schema, tensors: Q4TensorBundle, ?backend: IQ4Backend) =
  let resolvedBackend = defaultArg backend (Backend.create schema config)
  let mutable preparedWeight : IQ4PreparedWeight option = None
  let mutable disposed = false

  let resolveTargetDevice () =
    match config.RuntimeTarget with
    | Q4RuntimeTarget.Cpu -> "cpu"
    | Q4RuntimeTarget.Cuda idx -> sprintf "cuda:%d" idx
    | Q4RuntimeTarget.Auto ->
      if torch.cuda_is_available() then "cuda:0" else "cpu"

  let ensurePrepared () =
    match preparedWeight with
    | Some p -> p
    | None ->
      let bundle =
        {
          Weight = UnifiedMemory.applyMutablePolicy config.UnifiedMemoryPolicy tensors.Weight
          Scale = tensors.Scale |> Option.map (UnifiedMemory.applyMutablePolicy config.UnifiedMemoryPolicy)
          Absmax = tensors.Absmax |> Option.map (UnifiedMemory.applyMutablePolicy config.UnifiedMemoryPolicy)
          QuantMap = tensors.QuantMap |> Option.map (UnifiedMemory.applyMutablePolicy config.UnifiedMemoryPolicy)
        }
      let prepared = resolvedBackend.PrepareWeight(schema, bundle, resolveTargetDevice())
      preparedWeight <- Some prepared
      prepared

  member _.BackendName : string =
    resolvedBackend.Name

  member _.Forward(input: TorchSharp.torch.Tensor, ?outDtype: TorchSharp.torch.ScalarType) : TorchSharp.torch.Tensor =
    if disposed then
      raise (ObjectDisposedException("Q4Linear"))
    let prepared = ensurePrepared ()
    let policyAppliedInput = UnifiedMemory.applyPolicy config.UnifiedMemoryPolicy input
    let targetOutDtype = defaultArg outDtype policyAppliedInput.dtype
    resolvedBackend.Linear(policyAppliedInput, prepared, targetOutDtype)

  interface IDisposable with
    member _.Dispose() =
      if not disposed then
        disposed <- true
        match preparedWeight with
        | Some p ->
          p.Dispose()
          preparedWeight <- None
        | None -> ()
