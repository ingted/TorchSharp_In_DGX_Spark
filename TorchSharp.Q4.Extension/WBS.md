# TorchSharp.Q4.Extension WBS

## 1. 任務分解與追蹤

| WBS ID | 工作項目 | 輸出 | 對應 UseCase | 對應 TestCase | 狀態 |
|---|---|---|---|---|---|
| WBS-01 | 定義 Use Cases | `UseCase.md` | UC-01..UC-07 | 全部 | Done |
| WBS-02 | 定義 Test Cases | `TestCase.md` | UC-01..UC-07 | TC-01..TC-13 | Done |
| WBS-03 | 系統設計 | `SD.md` | UC-01..UC-07 | 全部 | Done |
| WBS-04 | 工作拆解與驗收映射 | `WBS.md` | UC-01..UC-07 | TC-01..TC-13 | Done |
| WBS-05 | 建立 F# 專案骨架 | `TorchSharp.Q4.Extension.fsproj` | UC-03 | TC-06 | Done |
| WBS-06 | 定義公開型別簽名 | `Types.fsi` | UC-01/02/04/05 | TC-01..TC-10 | Done |
| WBS-07 | 定義 schema 簽名 | `Schema.fsi` | UC-01/06 | TC-01/02/03/11/12 | Done |
| WBS-08 | 定義 backend 簽名 | `Backend.fsi` | UC-02/05/06 | TC-04/05/09/10/12 | Done |
| WBS-09 | 定義 native interop 簽名 | `NativeInterop.fsi` | UC-03/06 | TC-05/06/12 | Done |
| WBS-10 | 定義 UM policy 簽名 | `UnifiedMemory.fsi` | UC-04/07 | TC-07/08/13 | Done |
| WBS-11 | 定義 session/linear 簽名 | `Session.fsi`, `Q4Linear.fsi` | UC-05/07 | TC-09/10/13 | Done |
| WBS-12 | 撰寫預期使用腳本 | `UseCase.fsx` | UC-01..UC-07 | N/A | Done |
| WBS-13 | 撰寫預期測試腳本 | `TestCase.fsx` | UC-01..UC-07 | TC-01..TC-13 | Done |
| WBS-14 | 實作 Schema Detector | code | UC-01/06 | TC-01/02/03/11/12 | Pending |
| WBS-15 | 實作 Backend Dispatcher | code | UC-02/06 | TC-04/05/12 | Pending |
| WBS-16 | 實作 NF4 backend | code | UC-01/02/05 | TC-01/04/09/10 | Pending |
| WBS-17 | 實作 NVFP4 backend | code | UC-01/02/05 | TC-02/04/09/10 | Pending |
| WBS-18 | 實作 UM policy manager | code | UC-04/07 | TC-07/08/13 | Pending |
| WBS-19 | 建立 actor-safe mutation contract | code+doc | UC-07 | TC-13 | Pending |
| WBS-20 | E2E 驗證與性能基準 | report | UC-01..UC-07 | TC-01..TC-13 | Pending |

## 2. 驗收門檻
- 所有 `Done` 項目文件可讀且可追蹤。
- 所有 `Pending` 項目必須標註至少一個 UseCase 與一個 TestCase。
- 進入實作階段前，需先確認 `UC/TC/SD/WBS` 無關鍵遺漏。
