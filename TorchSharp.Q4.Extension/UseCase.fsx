#if INTERACTIVE
#r "nuget: FAkka.TorchSharp.DGX, 26.1.0-py3.5"
// 預期未來改為編譯後 DLL 引用
// #r "./bin/Debug/net10.0/TorchSharp.Q4.Extension.dll"
#endif

(***
UseCase.fsx (預期版)
目的: 展示未來 TorchSharp.Q4.Extension 的預期使用方式。
此檔目前為規格腳本，不保證可直接執行。
***)

open System

printfn "[UC] TorchSharp.Q4.Extension expected usage script"
printfn "[UC-01] 以 schema 自動判斷 NF4/NVFP4 backend"
printfn "[UC-02] 可手動覆寫 backend 與 compute path"
printfn "[UC-04] 可指定 Unified Memory policy"
printfn "[UC-05] 使用 Q4Linear PrepareWeight + Forward"
printfn "[UC-07] Actor runtime 可控 session 資源生命週期"

(*
// 預期 API 草稿:
open TorchSharp.Q4.Extension
open TorchSharp

let cfg =
  {
    BackendOverride = None
    ComputePath = Q4ComputePath.KernelOrFallback
    RuntimeTarget = Q4RuntimeTarget.Auto
    UnifiedMemoryPolicy = UnifiedMemoryPolicy.PreferUnified
    EnableDiagnostics = true
  }

let schema = Schema.detectOrFail stateDict
let session = Session.create cfg schema
let linear = session.CreateLinear(bundle)
use input = torch.randn([| 1L; 2560L |], device = "cuda", dtype = torch.float16)
use out = linear.Forward(input)
printfn "out shape = %A" out.shape
*)
