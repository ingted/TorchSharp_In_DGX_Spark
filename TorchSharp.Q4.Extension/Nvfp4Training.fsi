namespace TorchSharp.Q4.Extension

open TorchSharp

module Nvfp4Training =
  val quantizePacked : input:TorchSharp.torch.Tensor -> TorchSharp.torch.Tensor * TorchSharp.torch.Tensor
  val dequantizePacked : qdata:TorchSharp.torch.Tensor -> scale:TorchSharp.torch.Tensor -> outDtype:TorchSharp.torch.ScalarType -> TorchSharp.torch.Tensor
  val steWeight : masterWeight:TorchSharp.torch.Tensor -> TorchSharp.torch.Tensor
  val linearSte : input:TorchSharp.torch.Tensor -> masterWeight:TorchSharp.torch.Tensor -> outDtype:TorchSharp.torch.ScalarType -> TorchSharp.torch.Tensor
