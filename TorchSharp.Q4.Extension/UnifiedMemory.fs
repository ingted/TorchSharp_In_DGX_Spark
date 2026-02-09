namespace TorchSharp.Q4.Extension

open System
open TorchSharp

module private UnifiedMemoryImpl =
  let notImpl<'T> (name: string) : 'T =
    raise (NotImplementedException(name))

module UnifiedMemory =
  let isSupported () : bool =
    UnifiedMemoryImpl.notImpl "UnifiedMemory.isSupported"

  let applyPolicy (policy: UnifiedMemoryPolicy) (tensor: TorchSharp.torch.Tensor) : TorchSharp.torch.Tensor =
    let _ = policy, tensor
    UnifiedMemoryImpl.notImpl "UnifiedMemory.applyPolicy"

  let applyMutablePolicy (policy: UnifiedMemoryPolicy) (tensor: TorchSharp.torch.Tensor) : TorchSharp.torch.Tensor =
    let _ = policy, tensor
    UnifiedMemoryImpl.notImpl "UnifiedMemory.applyMutablePolicy"
