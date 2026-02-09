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
