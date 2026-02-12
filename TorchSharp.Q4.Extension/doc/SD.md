# TorchSharp.Q4.Extension System Design

## English Version

### 1. Design Goals
- Unify NF4 / NVFP4 external API (`TorchSharp.Q4.Extension`).
- Keep multi-backend strategy internally, instead of assuming one universal kernel.
- Implement in F# first, with a replaceable C# adapter as fallback.
- Prioritize Actor-based SNN + Unified Memory as the main design driver.

### 2. Architecture Overview
- Layer A: Public API (F#)
  - Session, Schema, Q4Linear, Diagnostics
- Layer B: Dispatcher
  - Select backend/path by schema + policy + runtime capability
- Layer C: Backends
  - NF4 backend
  - NVFP4 backend
  - Dequant+Matmul fallback backend
- Layer D: Native Interop
  - F# P/Invoke bindings
  - Native library loading / symbol checks
- Layer E: Memory Policy
  - Unified Memory policy / mutable tensor contract

### 3. Core Components

#### 3.1 Schema Detector
- Input: `Map<string, Tensor>`
- Output: `Q4Schema`
- Rules:
  - `.absmax/.quant_map` -> NF4
  - `.qdata/.scale` -> NVFP4

#### 3.2 Backend Registry + Dispatcher
- Input: `Q4Schema`, `Q4SessionConfig`, runtime capabilities
- Output: `IQ4Backend`
- Policy:
  - `KernelOnly`: fallback not allowed
  - `KernelOrFallback`: degrade when unavailable
  - `DequantMatmulOnly`: validate logic correctness only

#### 3.3 Q4Linear Contract
- `PrepareWeight`: convert schema/tensor into backend-ready state
- `Forward`: execute Q4 inference path
- `Dispose`: release prepared/native resources

#### 3.5 NVFP4 Training Utility Contract
- `quantizePacked`: native FP4 quantize on CUDA; deterministic fallback on CPU.
- `dequantizePacked`: decode packed FP4 + scale into dense compute tensor.
- `steWeight`: preserve gradient to master weight via STE identity-gradient trick.
- `linearSte`: training forward path for autograd-compatible quantized behavior.

Lifecycle rules:
- Any non-return intermediate tensor in hot path must be bound by `use`.
- Avoid chained temporary-heavy expressions when fallback path is expected to run repeatedly.
- Device/dtype conversion branches must dispose temporary converted tensors.

#### 3.6 Cross-Repo Integration Contract (Training Apps)
- Downstream training apps that consume `Nvfp4Training.linearSte` must enforce explicit tensor lifecycle control in loss computation helpers.
- Minimum rule:
  - Any `target.to_type(...)` temporary must be disposed when not reused.
  - Any intermediate tensor from arithmetic/activation (`diff`, `abs`, etc.) must use explicit scoped disposal.
- This requirement is part of extension-level reliability because integration leaks invalidate lifecycle assumptions established in Layer C utility code.

#### 3.7 NVFP4 Native Interop Contract
- NVFP4 kernel path must call `libNVFP4.so` exports directly (`NVFP4_quantize`, `NVFP4_scaled_mm`) as the source-of-truth runtime path.
- `LibTorchSharp` THS FP4 wrappers are treated as optional compatibility probes only, not the canonical execution path.
- Backend capability check for NVFP4 kernel must validate NVFP4 export visibility from configured NVFP4 library candidates.

#### 3.4 Unified Memory Contract
- `Disabled`: no UM specialization
- `PreferUnified`: use UM if available, otherwise fallback
- `RequireUnified`: fail when unavailable

Managed path details (2026-02-12):
- Native exports in `libNVFP4.so`:
  - `NVFP4_can_use_managed`
  - `NVFP4_is_managed_tensor`
  - `NVFP4_to_managed`
  - `NVFP4_managed_prefetch`
- `UnifiedMemory.applyPolicy`:
  - promotes tensors to managed memory when UM is available and policy allows.
- `UnifiedMemory.applyMutablePolicy`:
  - clone+detach first (actor-safe), then promote to managed memory.
- `Q4Linear.Forward`:
  - uses `applyInputPolicy` for activation path (PreferUnified is zero-copy-first).
  - disposes temporary input tensor only when a new tensor is actually allocated (e.g., RequireUnified conversion).

### 4. F# and C# Interaction Strategy

#### 4.1 Preferred Strategy (project target)
- F# directly P/Invokes native symbols.
- F# directly handles required reflection (e.g., tensor handle bridge).

#### 4.2 Fallback Strategy
- If runtime limits make pure F# unstable, allow the thinnest C# adapter.
- The adapter is replaceable backend glue only and must not own the public API.

### 5. Failure Modes and Diagnostics
- Incomplete schema
- Kernel unavailable
- Alignment mismatch
- Unified Memory policy not satisfied
- Forced backend override conflicts with real capability
- Tensor lifecycle regression in repeated training calls (wrapper GC lag vs native pressure)
- Cross-repo lifecycle mismatch (extension fixed, downstream training loop still leaking temporaries)

Diagnostics must include at least:
- `format`, `backend`, `computePath`, `fallbackReason`, `nativeLoadState`

### 6. Alignment with SNN/Actor
- Session must have explicit resource lifecycle to avoid unknown cross-actor sharing.
- Mutable tensors must define an update contract (single-writer multi-reader/version stamp/snapshot).
- UM policy must be controllable by actor runtime.

### 7. Deliverables in This Version
- Docs: `doc/UseCase.md`, `doc/TestCase.md`, `doc/SA.md`, `doc/SD.md`, `doc/WBS.md`, `doc/DevLog.md`
- Scripts: `UseCase.fsx`, `TestCase.fsx`
- Project: `TorchSharp.Q4.Extension` F# skeleton + `.fsi` signatures
- Includes NVFP4 training utility and issue-driven hardening tasks (WBS-21+)
- Includes cross-repo lifecycle contract and integration fix tracking (WBS-27)

---

## 中文版

## 1. 設計目標
- 統一 NF4 / NVFP4 對外 API（`TorchSharp.Q4.Extension`）。
- 內部保持多 backend 策略，而非假設單一通用 kernel。
- F# 優先實作，保留 C# adapter 作可替換 fallback。
- 以 Actor-based SNN + Unified Memory 為第一優先設計驅動。

## 2. 架構總覽
- Layer A: Public API (F#)
  - Session, Schema, Q4Linear, Diagnostics
- Layer B: Dispatcher
  - 依 schema + policy + runtime capability 選 backend/path
- Layer C: Backends
  - NF4 backend
  - NVFP4 backend
  - Dequant+Matmul fallback backend
- Layer D: Native Interop
  - F# P/Invoke bindings
  - Native library loading / symbol checks
- Layer E: Memory Policy
  - Unified Memory policy / mutable tensor contract

## 3. 核心元件

### 3.1 Schema Detector
- 輸入: `Map<string, Tensor>`
- 輸出: `Q4Schema`
- 規則:
  - `.absmax/.quant_map` -> NF4
  - `.qdata/.scale` -> NVFP4

### 3.2 Backend Registry + Dispatcher
- 輸入: `Q4Schema`, `Q4SessionConfig`, runtime capabilities
- 輸出: `IQ4Backend`
- 策略:
  - `KernelOnly`: 不允許 fallback
  - `KernelOrFallback`: 不可用時降級
  - `DequantMatmulOnly`: 僅驗證邏輯正確性

### 3.3 Q4Linear Contract
- `PrepareWeight`：將 schema/tensor 轉 backend-ready 狀態
- `Forward`：執行 Q4 路徑推理
- `Dispose`：釋放 prepared/native 資源

### 3.5 NVFP4 Training Utility Contract
- `quantizePacked`：CUDA 走 native FP4 量化；CPU 走 deterministic fallback。
- `dequantizePacked`：將 packed FP4 + scale 還原為 dense 計算 tensor。
- `steWeight`：以 STE identity-gradient 技巧保留 master weight 梯度。
- `linearSte`：提供訓練用、可 autograd 的量化前向路徑。

生命週期規範：
- 熱路徑中所有非回傳的中間 tensor 必須用 `use` 綁定。
- fallback 路徑避免過多鏈式臨時物件。
- device/dtype 轉換分支建立的暫存 tensor 必須顯式釋放。

### 3.6 跨 Repo 整合契約（Training Apps）
- 下游訓練應用若使用 `Nvfp4Training.linearSte`，其 loss 計算輔助函式必須採用顯式 tensor 生命週期管理。
- 最低要求：
  - `target.to_type(...)` 產生的暫存 tensor，在不重用時必須釋放。
  - 算術/活化中間值（`diff`、`abs` 等）必須在局部作用域內顯式釋放。
- 這是 extension 層可靠性的一部分；若下游洩漏，會破壞 Layer C 已建立的生命週期假設。

### 3.7 NVFP4 Native Interop 契約
- NVFP4 kernel 路徑必須直接呼叫 `libNVFP4.so` 匯出（`NVFP4_quantize`、`NVFP4_scaled_mm`），作為實際運行主路徑。
- `LibTorchSharp` 的 THS FP4 包裝僅作為相容性探測，不作為 canonical 執行路徑。
- NVFP4 kernel backend 的能力檢查，必須檢驗已配置 NVFP4 library 候選路徑上的匯出可見性。

### 3.4 Unified Memory Contract
- `Disabled`: 不做 UM 特化
- `PreferUnified`: 可用則使用 UM，不可用可 fallback
- `RequireUnified`: 不可用即 fail

managed 路徑細節（2026-02-12）：
- `libNVFP4.so` 新增匯出：
  - `NVFP4_can_use_managed`
  - `NVFP4_is_managed_tensor`
  - `NVFP4_to_managed`
  - `NVFP4_managed_prefetch`
- `UnifiedMemory.applyPolicy`：
  - 在 UM 可用且策略允許時，將 tensor 升級為 managed memory。
- `UnifiedMemory.applyMutablePolicy`：
  - 先 `clone+detach`（維持 actor-safe），再升級為 managed memory。
- `Q4Linear.Forward`：
  - activation 路徑改用 `applyInputPolicy`（PreferUnified 採 zero-copy 優先）。
  - 僅在真的產生新 tensor（例如 RequireUnified 轉換）時，forward 結束後顯式釋放暫存輸入。

## 4. F# 與 C# 的互動策略

### 4.1 首選策略（本專案目標）
- F# 直接 P/Invoke native symbols。
- F# 直接處理必要 reflection（例如 tensor handle bridge）。

### 4.2 保底策略
- 若某些 runtime 限制造成 F# 單獨實作不穩，允許最薄 C# adapter。
- 但 adapter 只能當可替換 backend glue，不得主導 API。

## 5. 失敗模式與診斷
- Schema 不完整
- Kernel 不可用
- Alignment 不符
- Unified Memory policy 不滿足
- Backend 強制覆寫與實際能力衝突
- 重複訓練呼叫下的 tensor 生命周期回歸（wrapper GC 落後導致 native 壓力）
- 跨 repo 生命周期不一致（extension 已修，downstream training loop 仍有暫存洩漏）

Diagnostics 必須至少包含:
- `format`, `backend`, `computePath`, `fallbackReason`, `nativeLoadState`

## 6. 與 SNN/Actor 的對齊
- Session 內部需有明確資源生命週期，避免 actor 間未知共享。
- mutable tensor 需定義更新規約（單寫多讀/版本戳/快照）。
- UM policy 必須可被 actor runtime 控制。

## 7. 這一版交付內容
- 文件: `doc/UseCase.md`, `doc/TestCase.md`, `doc/SA.md`, `doc/SD.md`, `doc/WBS.md`, `doc/DevLog.md`
- 腳本: `UseCase.fsx`, `TestCase.fsx`
- 專案: `TorchSharp.Q4.Extension` F# skeleton + `.fsi` 簽名
- 包含 NVFP4 training utility 與 issue 驅動強化任務（WBS-21+）
- 包含跨 repo 生命週期契約與整合修正追蹤（WBS-27）
