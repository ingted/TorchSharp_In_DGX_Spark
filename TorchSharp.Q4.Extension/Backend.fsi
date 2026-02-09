namespace TorchSharp.Q4.Extension

module Backend =
  val create : schema:Q4Schema -> config:Q4SessionConfig -> IQ4Backend
  val tryCreate : schema:Q4Schema -> config:Q4SessionConfig -> Result<IQ4Backend, string>
  val listAvailable : unit -> string list
