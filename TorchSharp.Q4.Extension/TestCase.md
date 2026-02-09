# TorchSharp.Q4.Extension Test Cases

## 測試原則
- 先驗證 schema 與 backend 選擇，再驗證線性層運算。
- 先 deterministic correctness，再測性能與記憶體策略。
- 測試編號對應 `UseCase.md` 與 `WBS.md`。

## Test Matrix

### TC-01 NF4 schema detection
- 前置: 提供 `.weight/.absmax/.quant_map`
- 期望: `Schema.detect` 回傳 `NF4`
- 對應 UC: `UC-01`

### TC-02 NVFP4 schema detection
- 前置: 提供 `.qdata/.scale`
- 期望: `Schema.detect` 回傳 `NVFP4`
- 對應 UC: `UC-01`

### TC-03 Invalid schema diagnostics
- 前置: 欄位缺失或 shape 不符
- 期望: `Schema.validate` 回傳完整錯誤列表
- 對應 UC: `UC-01`

### TC-04 Manual backend override success
- 前置: schema 有效
- 期望: 可成功指定 backend + compute path
- 對應 UC: `UC-02`

### TC-05 Manual backend override failure path
- 前置: 強制 NVFP4 但 native 不可用
- 期望: 依 policy 回傳錯誤或 fallback
- 對應 UC: `UC-02`

### TC-06 No mandatory C# layer
- 前置: 僅載入 F# 專案
- 期望: API 可建立 session 並完成基本路徑（mock/stub）
- 對應 UC: `UC-03`

### TC-07 Unified memory policy: PreferUnified
- 前置: `PreferUnified`
- 期望: 走 Unified Memory 能力檢查 + 允許 fallback
- 對應 UC: `UC-04`

### TC-08 Unified memory policy: RequireUnified
- 前置: `RequireUnified` 且環境不支援
- 期望: fail fast 並回傳明確診斷
- 對應 UC: `UC-04`

### TC-09 Q4Linear prepare + forward contract
- 前置: 合法 schema + tensor
- 期望: `PrepareWeight` 與 `Forward` API 契約成立
- 對應 UC: `UC-05`

### TC-10 Shape/dtype guard
- 前置: 錯誤 shape 或 dtype
- 期望: fail fast，不產生 silent incorrect output
- 對應 UC: `UC-05`

### TC-11 Alignment validation
- 前置: 故意給不對齊維度
- 期望: 進 kernel 前被擋下並回傳診斷
- 對應 UC: `UC-06`

### TC-12 Diagnostics completeness
- 前置: backend 選擇與 fallback 混合情境
- 期望: diagnostics 含 format/backend/path/fallback reason
- 對應 UC: `UC-06`

### TC-13 Actor concurrent mutation contract
- 前置: 多 actor 競爭更新同一 tensor（模擬）
- 期望: 依規約可預期（鎖/版本戳/單寫多讀策略之一）
- 對應 UC: `UC-07`
