namespace TorchSharp.Q4.Extension

open TorchSharp

type Q4Linear =
  new : config:Q4SessionConfig * schema:Q4Schema * tensors:Q4TensorBundle * ?backend:IQ4Backend -> Q4Linear
  member BackendName : string
  member Forward : input:TorchSharp.torch.Tensor * ?outDtype:TorchSharp.torch.ScalarType -> TorchSharp.torch.Tensor
  interface System.IDisposable
