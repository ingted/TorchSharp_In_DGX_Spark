namespace TorchSharp.Q4.Extension

open System

module private BackendImpl =
  let notImpl<'T> (name: string) : 'T =
    raise (NotImplementedException(name))

module Backend =
  let create (schema: Q4Schema) (config: Q4SessionConfig) : IQ4Backend =
    let _ = schema, config
    BackendImpl.notImpl "Backend.create"

  let tryCreate (schema: Q4Schema) (config: Q4SessionConfig) : Result<IQ4Backend, string> =
    let _ = schema, config
    BackendImpl.notImpl "Backend.tryCreate"

  let listAvailable () : string list =
    BackendImpl.notImpl "Backend.listAvailable"
