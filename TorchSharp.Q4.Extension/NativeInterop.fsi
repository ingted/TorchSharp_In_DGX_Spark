namespace TorchSharp.Q4.Extension

open TorchSharp

module NativeInterop =
  type NativeLoadResult =
    {
      LibraryPath: string
      Loaded: bool
      Error: string option
    }

  val configure : nvfp4Path:string option * nf4Path:string option -> unit
  val loadNvfp4 : unit -> NativeLoadResult
  val loadNf4 : unit -> NativeLoadResult
  val isNvfp4Available : unit -> bool
  val isNf4Available : unit -> bool

  val hasLibTorchFp4Quantize : unit -> bool
  val hasLibTorchScaledMm : unit -> bool
  val fp4Quantize : input:TorchSharp.torch.Tensor -> TorchSharp.torch.Tensor * TorchSharp.torch.Tensor
  val scaledMmFp4 : mat1:TorchSharp.torch.Tensor -> mat2:TorchSharp.torch.Tensor -> scaleA:TorchSharp.torch.Tensor -> scaleB:TorchSharp.torch.Tensor -> outDtype:TorchSharp.torch.ScalarType -> TorchSharp.torch.Tensor
