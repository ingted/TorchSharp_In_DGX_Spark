# TorchSharp.Q4.Extension Test Cases

## English Version

### Testing Principles
- Verify schema and backend selection first, then verify linear-layer computation.
- Verify deterministic correctness first, then performance and memory policy.
- Test IDs map to `doc/UseCase.md` and `doc/WBS.md`.

### Test Matrix

#### TC-01 NF4 schema detection
- Preconditions: Provide `.weight/.absmax/.quant_map`
- Expected: `Schema.detect` returns `NF4`
- Related UC: `UC-01`

#### TC-02 NVFP4 schema detection
- Preconditions: Provide `.qdata/.scale`
- Expected: `Schema.detect` returns `NVFP4`
- Related UC: `UC-01`

#### TC-03 Invalid schema diagnostics
- Preconditions: Missing fields or mismatched shapes
- Expected: `Schema.validate` returns complete error list
- Related UC: `UC-01`

#### TC-04 Manual backend override success
- Preconditions: Valid schema
- Expected: Backend + compute path can be forced successfully
- Related UC: `UC-02`

#### TC-05 Manual backend override failure path
- Preconditions: Force NVFP4 while native backend is unavailable
- Expected: Return error or fallback based on policy
- Related UC: `UC-02`

#### TC-06 No mandatory C# layer
- Preconditions: Load only F# project
- Expected: API can create session and pass basic path (mock/stub)
- Related UC: `UC-03`

#### TC-07 Unified memory policy: PreferUnified
- Preconditions: `PreferUnified`
- Expected: Run UM capability check and allow fallback
- Related UC: `UC-04`

#### TC-08 Unified memory policy: RequireUnified
- Preconditions: `RequireUnified` with unsupported environment
- Expected: Fail fast with explicit diagnostics
- Related UC: `UC-04`

#### TC-09 Q4Linear prepare + forward contract
- Preconditions: Valid schema + tensor
- Expected: `PrepareWeight` and `Forward` API contract holds
- Related UC: `UC-05`

#### TC-10 Shape/dtype guard
- Preconditions: Invalid shape or dtype
- Expected: Fail fast without silent incorrect output
- Related UC: `UC-05`

#### TC-11 Alignment validation
- Preconditions: Intentionally misaligned dimensions
- Expected: Rejected before kernel execution with diagnostics
- Related UC: `UC-06`

#### TC-12 Diagnostics completeness
- Preconditions: Mixed backend-selection/fallback scenarios
- Expected: Diagnostics include format/backend/path/fallback reason
- Related UC: `UC-06`

#### TC-13 Actor concurrent mutation contract
- Preconditions: Simulate multiple actors racing on one tensor update
- Expected: Behavior follows contract predictably (e.g., lock/version stamp/single-writer multi-reader)
- Related UC: `UC-07`

#### TC-14 Native FP4 quantize probe
- Preconditions: CUDA + NVFP4 exports available
- Expected: `NativeInterop.fp4Quantize` returns valid qdata/scale shapes
- Related UC: `UC-03`, `UC-05`

#### TC-15 Native FP4 scaled-mm smoke
- Preconditions: CUDA + NVFP4 exports available
- Expected: `NativeInterop.scaledMmFp4` output is finite and shape-valid
- Related UC: `UC-03`, `UC-05`

#### TC-16 Kernel backend selection on CUDA
- Preconditions: NVFP4 schema + CUDA runtime target
- Expected: dispatcher selects `nvfp4-kernel`
- Related UC: `UC-02`

#### TC-17 Kernel-only reject on CPU
- Preconditions: NVFP4 schema + CPU runtime target + `KernelOnly`
- Expected: backend creation fails fast
- Related UC: `UC-02`, `UC-06`

#### TC-18 Packed quant/dequant lifecycle stress
- Preconditions: repeated CPU quantize/dequantize path
- Expected: finite outputs and stable lifecycle behavior
- Related UC: `UC-05`, `UC-07`

#### TC-19 STE gradient validity
- Preconditions: `Nvfp4Training.linearSte` with trainable parameter
- Expected: non-null finite gradients
- Related UC: `UC-05`

#### TC-20 Repeated-call stress
- Preconditions: repeat `linearSte` forward/backward loop
- Expected: no crash, finite gradients across loop
- Related UC: `UC-05`, `UC-07`

#### TC-21 Managed conversion API
- Preconditions: CUDA + managed exports available
- Expected: `toManagedTensor` returns managed tensor with preserved data/shape
- Related UC: `UC-04`, `UC-07`

#### TC-22 UM policy promotion (`TS_Q4_DISABLE_UM=0`)
- Preconditions: set `TS_Q4_DISABLE_UM=0`, apply mutable policy
- Expected: `applyMutablePolicy PreferUnified` promotes tensor to managed memory
- Related UC: `UC-04`, `UC-07`

---

## 中文版

## 測試原則
- 先驗證 schema 與 backend 選擇，再驗證線性層運算。
- 先 deterministic correctness，再測性能與記憶體策略。
- 測試編號對應 `doc/UseCase.md` 與 `doc/WBS.md`。

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

### TC-14 Native FP4 quantize probe
- 前置: CUDA + NVFP4 匯出可用
- 期望: `NativeInterop.fp4Quantize` 回傳合法 qdata/scale shape
- 對應 UC: `UC-03`, `UC-05`

### TC-15 Native FP4 scaled-mm smoke
- 前置: CUDA + NVFP4 匯出可用
- 期望: `NativeInterop.scaledMmFp4` 輸出 shape 正確且有限值
- 對應 UC: `UC-03`, `UC-05`

### TC-16 CUDA 上 kernel backend 選擇
- 前置: NVFP4 schema + CUDA runtime target
- 期望: dispatcher 選到 `nvfp4-kernel`
- 對應 UC: `UC-02`

### TC-17 CPU 上 kernel-only 拒絕
- 前置: NVFP4 schema + CPU runtime target + `KernelOnly`
- 期望: backend 建立 fail fast
- 對應 UC: `UC-02`, `UC-06`

### TC-18 Packed quant/dequant 生命週期壓測
- 前置: 重複 CPU quantize/dequantize 路徑
- 期望: 輸出有限值且生命週期穩定
- 對應 UC: `UC-05`, `UC-07`

### TC-19 STE 梯度有效性
- 前置: `Nvfp4Training.linearSte` + 可訓練參數
- 期望: 梯度非空且有限值
- 對應 UC: `UC-05`

### TC-20 重複呼叫壓測
- 前置: `linearSte` forward/backward 迴圈重複執行
- 期望: 無 crash、梯度全程有限
- 對應 UC: `UC-05`, `UC-07`

### TC-21 Managed conversion API
- 前置: CUDA + managed 匯出可用
- 期望: `toManagedTensor` 回傳 managed tensor，且資料/shape 保持一致
- 對應 UC: `UC-04`, `UC-07`

### TC-22 UM policy promotion（`TS_Q4_DISABLE_UM=0`）
- 前置: 設定 `TS_Q4_DISABLE_UM=0` 後套用 mutable policy
- 期望: `applyMutablePolicy PreferUnified` 能升級為 managed memory
- 對應 UC: `UC-04`, `UC-07`
