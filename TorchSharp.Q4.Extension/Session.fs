namespace TorchSharp.Q4.Extension

open System

[<Interface>]
type Q4Session =
  abstract member Config : Q4SessionConfig
  abstract member Schema : Q4Schema
  abstract member Backend : IQ4Backend
  abstract member CreateLinear : tensors:Q4TensorBundle -> Q4Linear

type private StubSession(config: Q4SessionConfig, schema: Q4Schema) =
  interface Q4Session with
    member _.Config = config
    member _.Schema = schema
    member _.Backend = raise (NotImplementedException("Q4Session.Backend"))
    member _.CreateLinear(tensors: Q4TensorBundle) =
      let _ = tensors
      raise (NotImplementedException("Q4Session.CreateLinear"))

module Session =
  let create (config: Q4SessionConfig) (schema: Q4Schema) : Q4Session =
    StubSession(config, schema) :> Q4Session
