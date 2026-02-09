namespace TorchSharp.Q4.Extension

open System
open TorchSharp

type Q4Linear(config: Q4SessionConfig, schema: Q4Schema, tensors: Q4TensorBundle, ?backend: IQ4Backend) =
  let _cfg = config
  let _schema = schema
  let _tensors = tensors
  let _backend = backend

  member _.BackendName : string =
    raise (NotImplementedException("Q4Linear.BackendName"))

  member _.Forward(input: TorchSharp.torch.Tensor, ?outDtype: TorchSharp.torch.ScalarType) : TorchSharp.torch.Tensor =
    let _ = input, outDtype
    raise (NotImplementedException("Q4Linear.Forward"))

  interface IDisposable with
    member _.Dispose() =
      raise (NotImplementedException("Q4Linear.Dispose"))
