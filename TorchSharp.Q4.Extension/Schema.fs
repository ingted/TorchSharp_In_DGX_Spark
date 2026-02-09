namespace TorchSharp.Q4.Extension

open System
open TorchSharp

module private SchemaImpl =
  let notImpl<'T> (name: string) : 'T =
    raise (NotImplementedException(name))

module Schema =
  let detect (tensors: Map<string, TorchSharp.torch.Tensor>) : Q4Schema option =
    let _ = tensors
    SchemaImpl.notImpl "Schema.detect"

  let detectOrFail (tensors: Map<string, TorchSharp.torch.Tensor>) : Q4Schema =
    let _ = tensors
    SchemaImpl.notImpl "Schema.detectOrFail"

  let validate (schema: Q4Schema) (tensors: Map<string, TorchSharp.torch.Tensor>) : Result<unit, string list> =
    let _ = schema, tensors
    SchemaImpl.notImpl "Schema.validate"
