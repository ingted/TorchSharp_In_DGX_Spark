# TorchSharp.Q4.Extension DevLog

## English

### Repository Snapshot
- Repo: `/workspace/TorchSharp_In_DGX_Spark_fp4`
- Branch: `26.1.0-py3.1-fp4.0.2`
- Pre-change HEAD (2026-02-10T02:02:19Z): `94f4848`
- Focus area: `TorchSharp.Q4.Extension`
- Runtime baseline:
  - `FAkka.TorchSharp.DGX` `26.1.0-py3.6`
  - Native bridge: `libNVFP4.so` + LibTorch FP4 exports (`THSFP4_quantize`, `THSTensor_scaled_mm`)

### Tech Stack
- Language: F# (.NET 10)
- Core libs:
  - TorchSharp (`FAkka.TorchSharp.DGX`)
  - Native interop via P/Invoke
- Project scope:
  - Generic Q4 extension (NF4/NVFP4)
  - Training helper path (`Nvfp4Training`) with STE-compatible behavior

### Intake from `notes/00001.txt`
- Confirmed relevant issues for this repo:
  - I-21: `decodeToIndices` temporary tensor lifecycle hardening
  - I-22: `dequantizePacked` conversion branch temporary disposal
  - I-23: `steWeight` explicit intermediate disposal
  - I-24: fallback quantization chain lifecycle hardening
  - I-25: codebook reuse/caching optimization
  - I-26: add repeated-call stress test
- External tracked issue (out of scope here):
  - X-01: downstream trainer `scalarLoss` temporary disposal in app repo

### Change Policy
- Document-first:
  - `WBS.md` adds issue-driven backlog (`WBS-21`..`WBS-27`)
  - `SA.md` formalizes analysis and scope boundary
  - `SD.md` adds lifecycle rules for NVFP4 training utility
- Implementation follows WBS order and is recorded below with commit IDs.

### Implementation Log
- 2026-02-10T02:02:19Z
  - Initialized issue mapping from `notes/00001.txt`.
  - Added/updated planning docs (`WBS`, `SA`, `SD`).
  - Next: implement WBS-21..26 and run `TestCase.fsx`.
- 2026-02-10T02:05:17Z
  - Completed WBS-21..WBS-26 implementation in `Nvfp4Training.fs` and `TestCase.fsx`.
  - Implementation details:
    - Added explicit `use` ownership in `decodePacked`/`decodeToIndices`.
    - Refactored fallback quantization path to reduce hidden chained temporaries.
    - Added explicit temporary lifecycle control in `dequantizePacked`.
    - Made `steWeight` intermediate tensors explicit (`diff`, `diffDetached`).
    - Added per-device NVFP4 codebook cache (`ConcurrentDictionary`).
    - Added `TC-20` repeated-call stress test.
  - Validation:
    - `dotnet build -c Release` (pass)
    - `dotnet fsi TestCase.fsx` (TC-01..TC-20 pass; CUDA-specific tests skipped when CUDA unavailable)
  - Warning observed:
    - environment-level CUDA warning during test startup (`cudaGetDeviceCount` OS call issue), does not affect CPU test pass.
- 2026-02-10T02:06:00Z
  - External issue tracking kept as `WBS-27`:
  - downstream trainer `scalarLoss` temp disposal belongs to app repo, not extension repo.
- 2026-02-10T02:29:00Z
  - WBS-27 closed by cross-repo integration fix.
  - Downstream repo fix:
    - Repo: `/workspace/Qwen3-4B-Instruct-2507-TorchSharp.fs`
    - Commit: `1bbb8d4`
    - Change: `Trainer.fs` `scalarLoss` now disposes temporary tensors explicitly (`target.to_type` temp + `diff` + `abs` intermediate).
  - Validation (downstream):
    - `dotnet build -c Release` (pass)
    - `dotnet fsi scripts/Tests.fsx` (all pass)
  - Extension docs synced:
    - `doc/SD.md` added cross-repo lifecycle integration contract.
    - `doc/WBS.md` WBS-27 status changed to `Done`.

### Line-by-Line Mapping (notes/00001)
| Note Topic | Action | Status | Evidence |
|---|---|---|---|
| `decodeToIndices` low/high lifetime risk | Added explicit scoped disposal | Done | `ac45cab` |
| `dequantizePacked` temporary branch pressure | Added explicit conversion-branch lifecycle control | Done | `ac45cab` |
| `steWeight` intermediate temp clarity | Added explicit `diff` / `diffDetached` scoped disposal | Done | `ac45cab` |
| fallback quantization chain temporary pressure | Refactored chained ops into explicit scoped temps | Done | `ac45cab` |
| codebook recreation overhead | Added per-device codebook cache | Done | `ac45cab` |
| missing repeated-call lifecycle regression test | Added `TC-20` stress test | Done | `ac45cab` |
| downstream `scalarLoss` temporary leak risk | Patched downstream trainer and validated tests | Done | `1bbb8d4` |

---

## 中文

### Repository 快照
- Repo：`/workspace/TorchSharp_In_DGX_Spark_fp4`
- Branch：`26.1.0-py3.1-fp4.0.2`
- 變更前 HEAD（2026-02-10T02:02:19Z）：`94f4848`
- 本輪聚焦：`TorchSharp.Q4.Extension`
- Runtime baseline：
  - `FAkka.TorchSharp.DGX` `26.1.0-py3.6`
  - Native bridge：`libNVFP4.so` + LibTorch FP4 exports（`THSFP4_quantize`、`THSTensor_scaled_mm`）

### 技術棧
- 語言：F# (.NET 10)
- 核心元件：
  - TorchSharp（`FAkka.TorchSharp.DGX`）
  - 透過 P/Invoke 的 Native interop
- 專案定位：
  - 通用 Q4 extension（NF4/NVFP4）
  - 訓練輔助路徑（`Nvfp4Training`，支援 STE 行為）

### `notes/00001.txt` 問題收斂
- 本 repo 內確認相關：
  - I-21：`decodeToIndices` 暫存 tensor 生命週期強化
  - I-22：`dequantizePacked` 轉型分支暫存釋放
  - I-23：`steWeight` 中間值顯式釋放
  - I-24：fallback quantization 鏈式呼叫生命週期強化
  - I-25：codebook 快取/重用優化
  - I-26：新增重複呼叫壓力測試
- 外部追蹤（本 repo 不實作）：
  - X-01：下游應用 trainer 的 `scalarLoss` 暫存釋放問題

### 變更策略
- 文件先行：
  - `WBS.md` 新增 issue 驅動 backlog（`WBS-21`..`WBS-27`）
  - `SA.md` 明確化分析與範圍邊界
  - `SD.md` 新增 NVFP4 training utility 生命周期規範
- 後續依 WBS 順序實作，並在本檔補 commit ID。

### 實作日誌
- 2026-02-10T02:02:19Z
  - 完成 `notes/00001.txt` 的 issue 映射與文檔骨架更新。
  - 下一步：實作 WBS-21..26，執行 `TestCase.fsx` 驗證。
- 2026-02-10T02:05:17Z
  - 完成 WBS-21..WBS-26 的程式修正（`Nvfp4Training.fs`、`TestCase.fsx`）。
  - 實作細節：
    - 在 `decodePacked`/`decodeToIndices` 補上顯式 `use` 生命週期管理。
    - 重構 fallback 量化路徑，降低鏈式暫存物件壓力。
    - `dequantizePacked` 補齊轉型分支與中間值釋放控制。
    - `steWeight` 顯式管理中間 tensor（`diff`、`diffDetached`）。
    - 新增 per-device NVFP4 codebook 快取（`ConcurrentDictionary`）。
    - 新增 `TC-20` 重複呼叫壓力測試。
  - 驗證結果：
    - `dotnet build -c Release` 通過。
    - `dotnet fsi TestCase.fsx`（TC-01..TC-20 全通過；CUDA 不可用時 CUDA 相關 case 依設計 skip）。
  - 觀測到警告：
    - 測試啟動時有環境層 CUDA 警告（`cudaGetDeviceCount` OS call 問題），不影響 CPU 測試通過。
- 2026-02-10T02:06:00Z
  - 外部議題維持追蹤（`WBS-27`）：
  - 下游 trainer `scalarLoss` 暫存釋放屬 app repo，不在 extension repo 實作。
- 2026-02-10T02:29:00Z
  - WBS-27 已完成（跨 repo 整合修正）。
  - 下游 repo 修正：
    - Repo：`/workspace/Qwen3-4B-Instruct-2507-TorchSharp.fs`
    - Commit：`1bbb8d4`
    - 變更：`Trainer.fs` 的 `scalarLoss` 顯式釋放暫存 tensor（`target.to_type` 暫存 + `diff` + `abs` 中間值）。
  - 下游驗證：
    - `dotnet build -c Release` 通過
    - `dotnet fsi scripts/Tests.fsx` 全通過
  - extension 文檔同步：
    - `doc/SD.md` 新增跨 repo 生命周期整合契約。
    - `doc/WBS.md` 將 WBS-27 改為 `Done`。

### 逐條對照（notes/00001）
| 00001 主題 | 措施 | 狀態 | 證據 |
|---|---|---|---|
| `decodeToIndices` low/high 生命周期風險 | 補顯式釋放 | Done | `ac45cab` |
| `dequantizePacked` 暫存分支壓力 | 補顯式轉型分支生命周期控制 | Done | `ac45cab` |
| `steWeight` 中間值清晰化 | 補 `diff` / `diffDetached` 顯式釋放 | Done | `ac45cab` |
| fallback quantization 鏈式暫存壓力 | 拆分鏈式為顯式 scoped 暫存 | Done | `ac45cab` |
| codebook 重複建立成本 | 新增 per-device 快取 | Done | `ac45cab` |
| 缺少重複呼叫生命周期回歸測試 | 新增 `TC-20` 壓測 | Done | `ac45cab` |
| 下游 `scalarLoss` 暫存洩漏風險 | 修正下游 trainer 並驗證測試 | Done | `1bbb8d4` |
