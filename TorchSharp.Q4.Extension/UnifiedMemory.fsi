namespace TorchSharp.Q4.Extension

open TorchSharp

module UnifiedMemory =
  val isSupported : unit -> bool
  val isManagedTensor : tensor:TorchSharp.torch.Tensor -> bool
  val isPolicyEnabledByEnv : unit -> bool
  val tryPromoteToManaged : tensor:TorchSharp.torch.Tensor -> readMostly:bool -> prefetchDevice:int option -> TorchSharp.torch.Tensor
  val applyPolicy : policy:UnifiedMemoryPolicy -> tensor:TorchSharp.torch.Tensor -> TorchSharp.torch.Tensor
  val applyMutablePolicy : policy:UnifiedMemoryPolicy -> tensor:TorchSharp.torch.Tensor -> TorchSharp.torch.Tensor
