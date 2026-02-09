namespace TorchSharp.Q4.Extension

open System

[<Interface>]
type Q4Session =
  abstract member Config : Q4SessionConfig
  abstract member Schema : Q4Schema
  abstract member Backend : IQ4Backend
  abstract member CreateLinear : tensors:Q4TensorBundle -> Q4Linear

type private RuntimeSession(config: Q4SessionConfig, schema: Q4Schema, backend: IQ4Backend) =
  interface Q4Session with
    member _.Config = config
    member _.Schema = schema
    member _.Backend = backend
    member _.CreateLinear(tensors: Q4TensorBundle) =
      new Q4Linear(config, schema, tensors, backend)

module Session =
  let create (config: Q4SessionConfig) (schema: Q4Schema) : Q4Session =
    let backend = Backend.create schema config
    RuntimeSession(config, schema, backend) :> Q4Session
