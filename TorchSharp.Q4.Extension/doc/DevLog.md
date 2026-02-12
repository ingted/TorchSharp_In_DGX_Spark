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

### 2026-02-12T02:10:00Z
- Root cause found for NVFP4 inference quality drift in downstream pure F# path:
  - downstream `run-training.fsx` using `TorchSharp.Q4.Extension` produced gibberish while `run2.fsx` stayed coherent.
  - interop path mismatch confirmed:
    - old Q4 extension path: `LibTorchSharp` THS wrappers (`THSFP4_quantize`, `THSTensor_scaled_mm`)
    - known-good path: direct `libNVFP4.so` (`NVFP4_quantize`, `NVFP4_scaled_mm`)
- Implemented in `TorchSharp.Q4.Extension/NativeInterop.fs`:
  - switched FP4 quantize/scaled-mm calls to direct NVFP4 exports.
  - backend capability probe now checks NVFP4 export symbols on NVFP4 library candidates.
  - diagnostics/error text updated from THS/LibTorch wording to NVFP4 export wording.
- Integration validation (downstream):
  - `dotnet build -c Release /workspace/Qwen3-4B-Instruct-2507-TorchSharp.fs/...` pass
  - `dotnet fsi /workspace/fsann/alpha/runner-arm64-fp4/run-training.fsx --max-tokens 32 --temp 0 --top-p 1 --seed 123 --prompt "Write one short sentence about UFO and you."`
    - output recovered to coherent English sentence quality.

### 2026-02-12T02:10:00Z（中文）
- 已定位下游 pure F# 推論語意劣化的根因：
  - 下游 `run-training.fsx`（走 `TorchSharp.Q4.Extension`）輸出亂碼；
  - `run2.fsx`（既有路徑）輸出正常。
  - 差異在 native interop 路徑：
    - 舊版 Q4 extension：走 `LibTorchSharp` THS 包裝（`THSFP4_quantize`、`THSTensor_scaled_mm`）
    - 可用路徑：直接走 `libNVFP4.so`（`NVFP4_quantize`、`NVFP4_scaled_mm`）
- 已在 `TorchSharp.Q4.Extension/NativeInterop.fs` 實作修正：
  - FP4 量化與 scaled-mm 改為直接呼叫 NVFP4 匯出。
  - backend 能力探測改為檢查 NVFP4 library 候選路徑上的匯出符號。
  - 診斷/錯誤訊息同步更新為 NVFP4 匯出語意。
- 下游整合驗證：
  - `dotnet build -c Release /workspace/Qwen3-4B-Instruct-2507-TorchSharp.fs/...` 通過。
  - `run-training.fsx` 同 prompt 測試已回復可讀、語意合理英文句子。

### 2026-02-12T19:30:00Z
- UM path expansion completed (managed allocator integration).
- Native bridge updates (`/workspace/nvfp4_native/libNVFP4.cpp`):
  - added `NVFP4_can_use_managed`
  - added `NVFP4_is_managed_tensor`
  - added `NVFP4_to_managed`
  - added `NVFP4_managed_prefetch`
  - rebuilt `/workspace/nvfp4_native/libNVFP4.so`
- Extension updates:
  - `NativeInterop.fs/.fsi` expose managed capability/probe/conversion APIs.
  - `UnifiedMemory.fs/.fsi` now promotes tensors to managed memory when policy allows.
  - `Q4Linear.fs` disposes policy-created temporary input tensors after forward.
  - `Backend.diagnose` includes managed export/capability state.
- Validation:
  - `dotnet build -c Release TorchSharp.Q4.Extension.fsproj` pass
  - `dotnet fsi TestCase.fsx` pass (TC-01..TC-22)
  - New tests:
    - `TC-21`: direct managed conversion path
    - `TC-22`: `applyMutablePolicy` promotion under `TS_Q4_DISABLE_UM=0`

### 2026-02-12T19:30:00Z（中文）
- 完成 UM 路徑擴展（managed allocator 整合）。
- Native bridge 更新（`/workspace/nvfp4_native/libNVFP4.cpp`）：
  - 新增 `NVFP4_can_use_managed`
  - 新增 `NVFP4_is_managed_tensor`
  - 新增 `NVFP4_to_managed`
  - 新增 `NVFP4_managed_prefetch`
  - 已重編 `/workspace/nvfp4_native/libNVFP4.so`
- Extension 端更新：
  - `NativeInterop.fs/.fsi` 暴露 managed 能力探測與轉換 API。
  - `UnifiedMemory.fs/.fsi` 在策略允許下會將 tensor 升級為 managed memory。
  - `Q4Linear.fs` 在 forward 後釋放 policy 產生的暫存輸入 tensor。
  - `Backend.diagnose` 新增 managed export/capability 狀態。
- 驗證：
  - `dotnet build -c Release TorchSharp.Q4.Extension.fsproj` 通過
  - `dotnet fsi TestCase.fsx` 通過（TC-01..TC-22）
- 新增測試：
  - `TC-21`：直接 managed 轉換路徑
  - `TC-22`：`TS_Q4_DISABLE_UM=0` 下 `applyMutablePolicy` 升級驗證

### 2026-02-12T20:40:00Z
- Zero-copy audit triggered by `notes/00002.txt` and GB10 freeze report.
- Key anti-pattern found in runtime path:
  - `Q4Linear.Forward` used `UnifiedMemory.applyPolicy` on every input tensor.
  - Under `PreferUnified`, this could invoke managed conversion repeatedly on ephemeral activations (per-layer/per-step), causing avoidable copies and migration pressure.
- Fixes:
  - Added `UnifiedMemory.applyInputPolicy` (zero-copy-first for activations).
    - `PreferUnified`: no implicit conversion for non-managed input.
    - `RequireUnified`: still enforces managed input.
  - `Q4Linear.Forward` switched to `applyInputPolicy`.
  - `applyMutablePolicy` changed to no implicit clone (remove hidden large copies).
  - Added `TC-23` to assert `PreferUnified` input path does not allocate implicit copies.
- Validation:
  - `dotnet build -c Release TorchSharp.Q4.Extension.fsproj` pass
  - `dotnet fsi TestCase.fsx` pass (`TC-01..TC-23`)

### 2026-02-12T20:40:00Z（中文）
- 依 `notes/00002.txt` 與 GB10 當機回報，完成 zero-copy 稽核。
- 發現主要反模式：
  - `Q4Linear.Forward` 每次都對輸入做 `UnifiedMemory.applyPolicy`。
  - 在 `PreferUnified` 下，會對短生命週期 activation 重複做 managed 轉換，導致每層/每步額外拷貝與 migration 壓力。
- 修正：
  - 新增 `UnifiedMemory.applyInputPolicy`（activation 路徑 zero-copy 優先）：
    - `PreferUnified`：不對 non-managed 輸入做隱式轉換。
    - `RequireUnified`：仍強制 managed 條件。
  - `Q4Linear.Forward` 改用 `applyInputPolicy`。
  - `applyMutablePolicy` 改為不做隱式 clone（移除隱藏大拷貝）。
  - 新增 `TC-23` 驗證 `PreferUnified` 輸入路徑不產生隱式複本。
- 驗證：
  - `dotnet build -c Release TorchSharp.Q4.Extension.fsproj` 通過
  - `dotnet fsi TestCase.fsx` 通過（`TC-01..TC-23`）
