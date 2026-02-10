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
