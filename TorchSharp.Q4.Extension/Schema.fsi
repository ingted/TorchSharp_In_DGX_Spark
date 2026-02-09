namespace TorchSharp.Q4.Extension

open TorchSharp

module Schema =
  val detect : tensors:Map<string, TorchSharp.torch.Tensor> -> Q4Schema option
  val detectOrFail : tensors:Map<string, TorchSharp.torch.Tensor> -> Q4Schema
  val validate : schema:Q4Schema -> tensors:Map<string, TorchSharp.torch.Tensor> -> Result<unit, string list>
