# TorchSharp.Q4.Extension System Design

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

### 3.4 Unified Memory Contract
- `Disabled`: 不做 UM 特化
- `PreferUnified`: 可用則使用 UM，不可用可 fallback
- `RequireUnified`: 不可用即 fail

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

Diagnostics 必須至少包含:
- `format`, `backend`, `computePath`, `fallbackReason`, `nativeLoadState`

## 6. 與 SNN/Actor 的對齊
- Session 內部需有明確資源生命週期，避免 actor 間未知共享。
- mutable tensor 需定義更新規約（單寫多讀/版本戳/快照）。
- UM policy 必須可被 actor runtime 控制。

## 7. 這一版交付內容
- 文件: `UseCase.md`, `TestCase.md`, `SD.md`, `WBS.md`
- 腳本: `UseCase.fsx`, `TestCase.fsx`
- 專案: `TorchSharp.Q4.Extension` F# skeleton + `.fsi` 簽名
- 不含實際 kernel 算子實作
