#if INTERACTIVE
#r "nuget: FAkka.TorchSharp.DGX, 26.1.0-py3.5"
#endif

(***
TestCase.fsx (預期版)
目的: 對應 TestCase.md 的驗收腳本骨架。
此檔目前為測試規格草稿，不含真實 assert 實作。
***)

open System

let testCases =
  [
    "TC-01 NF4 schema detection"
    "TC-02 NVFP4 schema detection"
    "TC-03 Invalid schema diagnostics"
    "TC-04 Manual backend override success"
    "TC-05 Manual backend override failure path"
    "TC-06 No mandatory C# layer"
    "TC-07 Unified memory policy: PreferUnified"
    "TC-08 Unified memory policy: RequireUnified"
    "TC-09 Q4Linear prepare + forward contract"
    "TC-10 Shape/dtype guard"
    "TC-11 Alignment validation"
    "TC-12 Diagnostics completeness"
    "TC-13 Actor concurrent mutation contract"
  ]

printfn "[TC] Planned tests = %d" testCases.Length
for tc in testCases do
  printfn "[PENDING] %s" tc

(*
預期後續:
- 改成 Expecto / xUnit F# test project
- 依 TC 編號對應 Schema/Backend/UM/Actor 契約測試
- 新增 CUDA 能力探測與條件測試分流
*)
