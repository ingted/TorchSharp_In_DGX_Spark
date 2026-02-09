namespace TorchSharp.Q4.Extension

open System

module private NativeInteropImpl =
  let notImpl<'T> (name: string) : 'T =
    raise (NotImplementedException(name))

module NativeInterop =
  type NativeLoadResult =
    {
      LibraryPath: string
      Loaded: bool
      Error: string option
    }

  let configure (nvfp4Path: string option, nf4Path: string option) : unit =
    let _ = nvfp4Path, nf4Path
    NativeInteropImpl.notImpl "NativeInterop.configure"

  let loadNvfp4 () : NativeLoadResult =
    NativeInteropImpl.notImpl "NativeInterop.loadNvfp4"

  let loadNf4 () : NativeLoadResult =
    NativeInteropImpl.notImpl "NativeInterop.loadNf4"

  let isNvfp4Available () : bool =
    NativeInteropImpl.notImpl "NativeInterop.isNvfp4Available"

  let isNf4Available () : bool =
    NativeInteropImpl.notImpl "NativeInterop.isNf4Available"
