# TorchSharp.Q4.Extension System Analysis

## English Version

### 1. Context and Trigger
- Source of issues: `/workspace/TorchSharp_In_DGX_Spark_fp4/notes/00001.txt`
- Focus: NVFP4 training path in `Nvfp4Training.fs`, especially tensor lifecycle and temporary allocation pressure.
- Constraint: Keep extension reusable and model-agnostic. Model-specific fixes must stay outside this project.

### 2. Problem Statements
- SA-01: Potential tensor wrapper/native lifetime mismatch in chained operations (`decodeToIndices`, `dequantizePacked`, `steWeight`).
- SA-02: CPU fallback quantization path uses dense chained expressions; risk of temporary object pressure and delayed GC.
- SA-03: NVFP4 codebook is recreated repeatedly in hot paths; avoidable overhead.
- SA-04: Existing tests validate correctness but do not include repeated-call stress for lifecycle regressions.
- SA-05: One issue in note (`scalarLoss` temp disposal) belongs to downstream training app, not this extension. It must be tracked but not implemented here.

### 3. Scope
- In-scope:
  - `TorchSharp.Q4.Extension/Nvfp4Training.fs`
  - `TorchSharp.Q4.Extension/TestCase.fsx`
  - docs (`WBS.md`, `SD.md`, `DevLog.md`, this file)
- Out-of-scope:
  - `/workspace/Qwen3-4B-Instruct-2507-TorchSharp.fs` trainer implementation changes (tracked as external integration follow-up)

### 4. Technical Assumptions
- TorchSharp tensor wrappers require explicit disposal discipline in hot loops.
- Correctness tests (shape/finite/gradient) are necessary but not sufficient for lifecycle regressions.
- Adding small static caches (e.g., codebook per device) is acceptable if lifecycle is deterministic.

### 5. Required Outcomes
- RA-01: Remove avoidable temporary leakage risks in NVFP4 training utility path.
- RA-02: Preserve existing behavior and gradients (`linearSte` must keep gradient path to master weight).
- RA-03: Add repeat-call stress test to catch regression early.
- RA-04: Record every change with commit IDs in `DevLog.md`.

### 6. UM Expansion (2026-02-12)
- SA-06: Baseline UM policy existed, but did not allocate managed tensors explicitly.
- SA-07: `TS_Q4_DISABLE_UM=0` should not only toggle policy; it must drive real managed-memory tensor creation for reusable model tensors.
- SA-08: Need native capability probes (`can_use_managed`, pointer-type check) to avoid silent pseudo-UM behavior.
- SA-09: Avoid implicit per-forward activation conversion to managed memory under `PreferUnified`; this is a hidden copy anti-pattern on unified-memory systems.
- Required outcomes:
  - RA-05: Add native managed allocator bridge and expose it in `NativeInterop`.
  - RA-06: Promote mutable model tensors to managed memory in UM-enabled policy path.
  - RA-07: Add deterministic tests for managed conversion path.
  - RA-08: Add zero-copy-first activation policy and regression test.

---

## 中文版

## 1. 背景與觸發
- 問題來源：`/workspace/TorchSharp_In_DGX_Spark_fp4/notes/00001.txt`
- 聚焦範圍：`Nvfp4Training.fs` 的 NVFP4 訓練路徑，特別是 tensor 生命週期與暫存配置壓力。
- 約束：保持 extension 通用、與模型專案解耦；模型特定修正不放在本專案。

## 2. 問題定義
- SA-01：鏈式運算可能造成 tensor wrapper/native 生命周期不一致（`decodeToIndices`、`dequantizePacked`、`steWeight`）。
- SA-02：CPU fallback 量化路徑鏈式呼叫較密集，易造成暫存物件壓力與 GC 延遲。
- SA-03：NVFP4 codebook 在熱路徑重複建立，存在可避免的額外成本。
- SA-04：現有測試主要驗證正確性，缺少重複呼叫壓力測試，無法及早抓到生命週期回歸。
- SA-05：note 中 `scalarLoss` 暫存釋放屬於下游訓練專案，不在本 extension 實作範圍，但需要記錄追蹤。

## 3. 範圍界定
- 範圍內：
  - `TorchSharp.Q4.Extension/Nvfp4Training.fs`
  - `TorchSharp.Q4.Extension/TestCase.fsx`
  - 文件（`WBS.md`、`SD.md`、`DevLog.md`、本檔）
- 範圍外：
  - `/workspace/Qwen3-4B-Instruct-2507-TorchSharp.fs` 的 trainer 實作變更（僅做外部追蹤）

## 4. 技術假設
- TorchSharp 在熱迴圈中需要更嚴格的顯式釋放策略。
- 形狀/數值/梯度測試必要但不足，需加上重複呼叫壓力測試。
- 可接受小型快取（例如 per-device codebook），前提是生命週期可控。

## 5. 目標成果
- RA-01：消除 NVFP4 訓練工具路徑中可避免的暫存洩漏風險。
- RA-02：保持既有行為與梯度正確性（`linearSte` 必須維持對 master weight 的梯度路徑）。
- RA-03：新增 repeat-call 壓力測試以提前發現回歸。
- RA-04：所有變更與 commit ID 必須紀錄於 `DevLog.md`。

## 6. UM 擴展（2026-02-12）
- SA-06：原本已有 UM policy，但沒有真正用 managed allocator 建 tensor。
- SA-07：`TS_Q4_DISABLE_UM=0` 不該只是策略開關，需驅動可重用模型 tensor 的 managed-memory 建立。
- SA-08：需補 native 能力探測（`can_use_managed`、pointer 類型檢查），避免偽 UM 行為。
- SA-09：在 `PreferUnified` 下，必須避免每次 forward 將 activation 隱式轉 managed；這是 unified-memory 平台的隱藏拷貝反模式。
- 目標補充：
  - RA-05：新增 native managed allocator bridge，並在 `NativeInterop` 暴露。
  - RA-06：在 UM 啟用策略路徑中，將 mutable model tensors 升級為 managed memory。
  - RA-07：補齊 managed conversion 的可重現測試。
  - RA-08：補齊 activation zero-copy-first 策略與回歸測試。
