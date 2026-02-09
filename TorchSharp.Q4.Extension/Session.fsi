namespace TorchSharp.Q4.Extension

[<Interface>]
type Q4Session =
  abstract member Config : Q4SessionConfig
  abstract member Schema : Q4Schema
  abstract member Backend : IQ4Backend
  abstract member CreateLinear : tensors:Q4TensorBundle -> Q4Linear

module Session =
  val create : config:Q4SessionConfig -> schema:Q4Schema -> Q4Session
