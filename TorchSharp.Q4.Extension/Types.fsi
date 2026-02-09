namespace TorchSharp.Q4.Extension

open System
open TorchSharp

type QuantFormat =
  | NF4
  | NVFP4

type Q4ComputePath =
  | Auto
  | KernelOnly
  | KernelOrFallback
  | DequantMatmulOnly

type Q4RuntimeTarget =
  | Auto
  | Cpu
  | Cuda of deviceIndex:int

type UnifiedMemoryPolicy =
  | Disabled
  | PreferUnified
  | RequireUnified

type Q4Schema =
  {
    Format: QuantFormat
    WeightKey: string
    ScaleKey: string option
    AbsmaxKey: string option
    QuantMapKey: string option
    ExtraKeys: string list
  }

type Q4SessionConfig =
  {
    BackendOverride: string option
    ComputePath: Q4ComputePath
    RuntimeTarget: Q4RuntimeTarget
    UnifiedMemoryPolicy: UnifiedMemoryPolicy
    EnableDiagnostics: bool
  }

type Q4TensorBundle =
  {
    Weight: TorchSharp.torch.Tensor
    Scale: TorchSharp.torch.Tensor option
    Absmax: TorchSharp.torch.Tensor option
    QuantMap: TorchSharp.torch.Tensor option
  }

type Q4Diagnostics =
  {
    Format: QuantFormat
    Backend: string
    ComputePath: Q4ComputePath
    NativeLoadState: string
    FallbackReason: string option
  }

type IQ4PreparedWeight =
  inherit IDisposable
  abstract member Format: QuantFormat
  abstract member Device: string
  abstract member DebugName: string

type IQ4Backend =
  abstract member Name: string
  abstract member Supports: schema:Q4Schema * config:Q4SessionConfig -> bool
  abstract member PrepareWeight: schema:Q4Schema * tensors:Q4TensorBundle * device:string -> IQ4PreparedWeight
  abstract member Linear: input:TorchSharp.torch.Tensor * prepared:IQ4PreparedWeight * outDtype:TorchSharp.torch.ScalarType -> TorchSharp.torch.Tensor
