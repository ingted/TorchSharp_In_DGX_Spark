namespace TorchSharp.Q4.Extension

open System
open System.Runtime.InteropServices

module private NativeInteropImpl =
  type NativeLoadResultInternal =
    {
      LibraryPath: string
      Loaded: bool
      Error: string option
    }

  let mutable private nvfp4OverridePath : string option = None
  let mutable private nf4OverridePath : string option = None

  let configurePaths (nvfp4Path: string option) (nf4Path: string option) =
    nvfp4OverridePath <- nvfp4Path
    nf4OverridePath <- nf4Path

  let candidatePaths (overridePath: string option) (envVar: string) (defaults: string list) =
    let envPath = Environment.GetEnvironmentVariable(envVar) |> Option.ofObj
    match overridePath, envPath with
    | Some p, _ -> [ p ]
    | None, Some p when not (String.IsNullOrWhiteSpace(p)) -> p :: defaults
    | None, _ -> defaults

  let tryLoadOne (candidate: string) : Result<unit, string> =
    try
      let mutable handle = IntPtr.Zero
      if NativeLibrary.TryLoad(candidate, &handle) then
        NativeLibrary.Free(handle)
        Ok ()
      else
        Error "dynamic loader returned false"
    with ex ->
      Error ex.Message

  let loadFirst (candidates: string list) : NativeLoadResultInternal =
    let mutable firstError : string option = None
    let rec loop (items: string list) =
      match items with
      | [] ->
        {
          LibraryPath = candidates |> List.tryHead |> Option.defaultValue ""
          Loaded = false
          Error = firstError
        }
      | path :: rest ->
        match tryLoadOne path with
        | Ok () ->
          {
            LibraryPath = path
            Loaded = true
            Error = None
          }
        | Error err ->
          if firstError.IsNone then
            firstError <- Some (sprintf "%s -> %s" path err)
          loop rest
    loop candidates

  let loadNvfp4 () =
    let candidates =
      candidatePaths
        nvfp4OverridePath
        "NVFP4_LIB_PATH"
        [ "/workspace/nvfp4_native/libNVFP4.so"; "libNVFP4.so" ]
    loadFirst candidates

  let loadNf4 () =
    let candidates = candidatePaths nf4OverridePath "NF4_LIB_PATH" [ "libbitsandbytes.so"; "libNF4.so" ]
    loadFirst candidates

module NativeInterop =
  type NativeLoadResult =
    {
      LibraryPath: string
      Loaded: bool
      Error: string option
    }

  let private toPublicResult (r: NativeInteropImpl.NativeLoadResultInternal) : NativeLoadResult =
    {
      LibraryPath = r.LibraryPath
      Loaded = r.Loaded
      Error = r.Error
    }

  let configure (nvfp4Path: string option, nf4Path: string option) : unit =
    NativeInteropImpl.configurePaths nvfp4Path nf4Path

  let loadNvfp4 () : NativeLoadResult =
    NativeInteropImpl.loadNvfp4 () |> toPublicResult

  let loadNf4 () : NativeLoadResult =
    NativeInteropImpl.loadNf4 () |> toPublicResult

  let isNvfp4Available () : bool =
    (loadNvfp4 ()).Loaded

  let isNf4Available () : bool =
    (loadNf4 ()).Loaded
