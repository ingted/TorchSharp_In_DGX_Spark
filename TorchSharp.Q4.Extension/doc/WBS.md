# TorchSharp.Q4.Extension WBS

## English Version

### 1. Work Breakdown and Tracking

| WBS ID | Work Item | Output | Related UseCase | Related TestCase | Status |
|---|---|---|---|---|---|
| WBS-01 | Define Use Cases | `doc/UseCase.md` | UC-01..UC-07 | All | Done |
| WBS-02 | Define Test Cases | `doc/TestCase.md` | UC-01..UC-07 | TC-01..TC-13 | Done |
| WBS-03 | System Design | `doc/SD.md` | UC-01..UC-07 | All | Done |
| WBS-04 | Work decomposition and acceptance mapping | `doc/WBS.md` | UC-01..UC-07 | TC-01..TC-13 | Done |
| WBS-05 | Build F# project skeleton | `TorchSharp.Q4.Extension.fsproj` | UC-03 | TC-06 | Done |
| WBS-06 | Define public type signatures | `Types.fsi` | UC-01/02/04/05 | TC-01..TC-10 | Done |
| WBS-07 | Define schema signatures | `Schema.fsi` | UC-01/06 | TC-01/02/03/11/12 | Done |
| WBS-08 | Define backend signatures | `Backend.fsi` | UC-02/05/06 | TC-04/05/09/10/12 | Done |
| WBS-09 | Define native interop signatures | `NativeInterop.fsi` | UC-03/06 | TC-05/06/12 | Done |
| WBS-10 | Define UM policy signatures | `UnifiedMemory.fsi` | UC-04/07 | TC-07/08/13 | Done |
| WBS-11 | Define session/linear signatures | `Session.fsi`, `Q4Linear.fsi` | UC-05/07 | TC-09/10/13 | Done |
| WBS-12 | Write expected usage script | `UseCase.fsx` | UC-01..UC-07 | N/A | Done |
| WBS-13 | Write expected test script | `TestCase.fsx` | UC-01..UC-07 | TC-01..TC-13 | Done |
| WBS-14 | Implement Schema Detector | `Schema.fs` | UC-01/06 | TC-01/02/03/11/12 | Done |
| WBS-15 | Implement Backend Dispatcher | `Backend.fs` | UC-02/06 | TC-04/05/12 | Done |
| WBS-16 | Implement NF4 backend | `Backend.fs` | UC-01/02/05 | TC-01/04/09/10 | Done (Baseline) |
| WBS-17 | Implement NVFP4 backend | `Backend.fs` | UC-01/02/05 | TC-02/04/09/10 | Done (Baseline) |
| WBS-18 | Implement UM policy manager | `UnifiedMemory.fs` | UC-04/07 | TC-07/08/13 | Done (Baseline) |
| WBS-19 | Build actor-safe mutation contract | `UnifiedMemory.fs`, `Q4Linear.fs`, `Session.fs` | UC-07 | TC-13 | Done (Baseline) |
| WBS-20 | E2E validation and performance baseline | `TestCase.fsx`, `doc/E2E_Baseline.md` | UC-01..UC-07 | TC-01..TC-13 | Done (Baseline) |

### 2. Acceptance Criteria
- All `Done` items must have readable and traceable documents.
- All `Pending` items must map to at least one UseCase and one TestCase.
- Before implementation starts, confirm no critical gaps across `UC/TC/SD/WBS`.

### 3. Issue-Driven Backlog (from `notes/00001.txt`)

| WBS ID | Work Item | Output | Related UseCase | Related TestCase | Status |
|---|---|---|---|---|---|
| WBS-21 | Fix tensor lifetime in `decodeToIndices` (`low/high` disposal) | `Nvfp4Training.fs` | UC-05/UC-07 | TC-18/TC-19 | Done |
| WBS-22 | Reduce temporary tensor leakage in `dequantizePacked` (`scale` conversion branch + intermediate disposal) | `Nvfp4Training.fs` | UC-05/UC-07 | TC-18 | Done |
| WBS-23 | Make `steWeight` explicit on intermediate disposal (`diff`, `diffDetached`) | `Nvfp4Training.fs` | UC-05 | TC-19 | Done |
| WBS-24 | Refactor fallback quantization chain into explicit `use` lifecycles | `Nvfp4Training.fs` | UC-05/UC-06 | TC-18 | Done |
| WBS-25 | Add NVFP4 codebook cache to reduce repeated allocation overhead | `Nvfp4Training.fs` | UC-05 | TC-18 | Done |
| WBS-26 | Add stress test for repeated `quantize/dequantize/linearSte` calls | `TestCase.fsx` | UC-05/UC-07 | TC-20 (new) | Done |
| WBS-27 | Fix cross-repo integration issue: downstream trainer loss-temp disposal and contract sync | `doc/SD.md`, `doc/DevLog.md`, downstream `Trainer.fs` | UC-07 | Integration validation (`scripts/Tests.fsx` in downstream repo) | Done |
| WBS-28 | Align NVFP4 native interop to direct `libNVFP4.so` exports (`NVFP4_quantize`, `NVFP4_scaled_mm`) | `NativeInterop.fs`, `doc/SD.md`, `doc/DevLog.md` | UC-02/UC-03/UC-05 | TC-02/TC-05/TC-09 | Done |
| WBS-29 | Add managed-memory native exports (`NVFP4_to_managed`, capability/probe/prefetch) | `nvfp4_native/libNVFP4.cpp`, `NativeInterop.fs` | UC-03/UC-04/UC-07 | TC-21/TC-22 | Done |
| WBS-30 | Implement UM promotion policy in extension (`applyPolicy`/`applyMutablePolicy`) | `UnifiedMemory.fs`, `Q4Linear.fs` | UC-04/UC-07 | TC-07/TC-08/TC-13/TC-22 | Done |
| WBS-31 | Extend test coverage for managed path | `TestCase.fsx` | UC-04/UC-07 | TC-21/TC-22 | Done |
| WBS-32 | Zero-copy hardening for activation path (avoid per-forward implicit UM conversion) | `UnifiedMemory.fs`, `Q4Linear.fs` | UC-04/UC-05/UC-07 | TC-23 | Done |
| WBS-33 | Remove dual residency + extra copy in UM path (Q4Linear source ownership + managed zero-copy prepare + UM CPU-load path) | `Q4Linear.fs`, `Backend.fs`, downstream `InferenceBridge.fs`, `nvfp4_native/libNVFP4.cpp` | UC-04/UC-05/UC-07 | Integration run (`run-training2.fsx`) + build validation | Done |

---

## 中文版

## 1. 任務分解與追蹤

| WBS ID | 工作項目 | 輸出 | 對應 UseCase | 對應 TestCase | 狀態 |
|---|---|---|---|---|---|
| WBS-01 | 定義 Use Cases | `doc/UseCase.md` | UC-01..UC-07 | 全部 | Done |
| WBS-02 | 定義 Test Cases | `doc/TestCase.md` | UC-01..UC-07 | TC-01..TC-13 | Done |
| WBS-03 | 系統設計 | `doc/SD.md` | UC-01..UC-07 | 全部 | Done |
| WBS-04 | 工作拆解與驗收映射 | `doc/WBS.md` | UC-01..UC-07 | TC-01..TC-13 | Done |
| WBS-05 | 建立 F# 專案骨架 | `TorchSharp.Q4.Extension.fsproj` | UC-03 | TC-06 | Done |
| WBS-06 | 定義公開型別簽名 | `Types.fsi` | UC-01/02/04/05 | TC-01..TC-10 | Done |
| WBS-07 | 定義 schema 簽名 | `Schema.fsi` | UC-01/06 | TC-01/02/03/11/12 | Done |
| WBS-08 | 定義 backend 簽名 | `Backend.fsi` | UC-02/05/06 | TC-04/05/09/10/12 | Done |
| WBS-09 | 定義 native interop 簽名 | `NativeInterop.fsi` | UC-03/06 | TC-05/06/12 | Done |
| WBS-10 | 定義 UM policy 簽名 | `UnifiedMemory.fsi` | UC-04/07 | TC-07/08/13 | Done |
| WBS-11 | 定義 session/linear 簽名 | `Session.fsi`, `Q4Linear.fsi` | UC-05/07 | TC-09/10/13 | Done |
| WBS-12 | 撰寫預期使用腳本 | `UseCase.fsx` | UC-01..UC-07 | N/A | Done |
| WBS-13 | 撰寫預期測試腳本 | `TestCase.fsx` | UC-01..UC-07 | TC-01..TC-13 | Done |
| WBS-14 | 實作 Schema Detector | `Schema.fs` | UC-01/06 | TC-01/02/03/11/12 | Done |
| WBS-15 | 實作 Backend Dispatcher | `Backend.fs` | UC-02/06 | TC-04/05/12 | Done |
| WBS-16 | 實作 NF4 backend | `Backend.fs` | UC-01/02/05 | TC-01/04/09/10 | Done (Baseline) |
| WBS-17 | 實作 NVFP4 backend | `Backend.fs` | UC-01/02/05 | TC-02/04/09/10 | Done (Baseline) |
| WBS-18 | 實作 UM policy manager | `UnifiedMemory.fs` | UC-04/07 | TC-07/08/13 | Done (Baseline) |
| WBS-19 | 建立 actor-safe mutation contract | `UnifiedMemory.fs`, `Q4Linear.fs`, `Session.fs` | UC-07 | TC-13 | Done (Baseline) |
| WBS-20 | E2E 驗證與性能基準 | `TestCase.fsx`, `doc/E2E_Baseline.md` | UC-01..UC-07 | TC-01..TC-13 | Done (Baseline) |

## 2. 驗收門檻
- 所有 `Done` 項目文件可讀且可追蹤。
- 所有 `Pending` 項目必須標註至少一個 UseCase 與一個 TestCase。
- 進入實作階段前，需先確認 `UC/TC/SD/WBS` 無關鍵遺漏。

## 3. Issue 驅動 Backlog（來自 `notes/00001.txt`）

| WBS ID | 工作項目 | 輸出 | 對應 UseCase | 對應 TestCase | 狀態 |
|---|---|---|---|---|---|
| WBS-21 | 修正 `decodeToIndices` 的 tensor 生命週期（`low/high` 釋放） | `Nvfp4Training.fs` | UC-05/UC-07 | TC-18/TC-19 | Done |
| WBS-22 | 降低 `dequantizePacked` 暫存 tensor 洩漏風險（`scale` 分支與中間值釋放） | `Nvfp4Training.fs` | UC-05/UC-07 | TC-18 | Done |
| WBS-23 | `steWeight` 顯式管理中間值（`diff`、`diffDetached`） | `Nvfp4Training.fs` | UC-05 | TC-19 | Done |
| WBS-24 | 重構 fallback quantization 鏈式呼叫為顯式 `use` 生命週期 | `Nvfp4Training.fs` | UC-05/UC-06 | TC-18 | Done |
| WBS-25 | 加入 NVFP4 codebook 快取，降低重複配置成本 | `Nvfp4Training.fs` | UC-05 | TC-18 | Done |
| WBS-26 | 新增重複呼叫壓力測試（`quantize/dequantize/linearSte`） | `TestCase.fsx` | UC-05/UC-07 | TC-20（新增） | Done |
| WBS-27 | 修正跨 repo 整合議題：下游 trainer loss 暫存釋放並同步契約 | `doc/SD.md`、`doc/DevLog.md`、下游 `Trainer.fs` | UC-07 | 整合驗證（下游 repo `scripts/Tests.fsx`） | Done |
| WBS-28 | 對齊 NVFP4 native interop 為直接 `libNVFP4.so` 匯出（`NVFP4_quantize`、`NVFP4_scaled_mm`） | `NativeInterop.fs`、`doc/SD.md`、`doc/DevLog.md` | UC-02/UC-03/UC-05 | TC-02/TC-05/TC-09 | Done |
| WBS-29 | 新增 managed-memory native 匯出（`NVFP4_to_managed`、能力探測、prefetch） | `nvfp4_native/libNVFP4.cpp`、`NativeInterop.fs` | UC-03/UC-04/UC-07 | TC-21/TC-22 | Done |
| WBS-30 | 實作 extension 端 UM 升級策略（`applyPolicy`/`applyMutablePolicy`） | `UnifiedMemory.fs`、`Q4Linear.fs` | UC-04/UC-07 | TC-07/TC-08/TC-13/TC-22 | Done |
| WBS-31 | 擴充 managed 路徑測試覆蓋 | `TestCase.fsx` | UC-04/UC-07 | TC-21/TC-22 | Done |
| WBS-32 | Activation 路徑 zero-copy 強化（避免每次 forward 隱式 UM 轉換） | `UnifiedMemory.fs`、`Q4Linear.fs` | UC-04/UC-05/UC-07 | TC-23 | Done |
| WBS-33 | 消除 UM 路徑雙份駐留與額外拷貝（Q4Linear source 所有權釋放 + managed zero-copy prepare + UM CPU 載入路徑） | `Q4Linear.fs`、`Backend.fs`、下游 `InferenceBridge.fs`、`nvfp4_native/libNVFP4.cpp` | UC-04/UC-05/UC-07 | 整合執行（`run-training2.fsx`）+ build 驗證 | Done |
