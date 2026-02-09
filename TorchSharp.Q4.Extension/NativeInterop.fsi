namespace TorchSharp.Q4.Extension

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
