namespace TorchSharp.Q4.Extension

open TorchSharp

module UnifiedMemory =
  val isSupported : unit -> bool
  val applyPolicy : policy:UnifiedMemoryPolicy -> tensor:TorchSharp.torch.Tensor -> TorchSharp.torch.Tensor
  val applyMutablePolicy : policy:UnifiedMemoryPolicy -> tensor:TorchSharp.torch.Tensor -> TorchSharp.torch.Tensor
