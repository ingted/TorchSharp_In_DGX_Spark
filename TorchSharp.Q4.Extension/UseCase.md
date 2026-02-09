# TorchSharp.Q4.Extension Use Cases

## 0. 最高宗旨
本專案最高宗旨是支援 **Actor-based SNN** 在有限預算下，最大化 GB10 平台的 Unified Memory 優勢，讓 actor 能快速讀寫可變 tensor，再交由 GPU 計算。

## 1. 範圍與角色
- 使用者: F# 開發者（SNN/LLM/一般 TorchSharp 使用者）
- 系統: `TorchSharp.Q4.Extension`
- 後端: `NF4` / `NVFP4` kernel 與 fallback 路徑

## 2. Use Cases

### UC-01 自動識別 Q4 格式並選擇 backend
- 目標: 不要求呼叫端手動指定 `NF4` 或 `NVFP4`。
- 輸入: 權重 tensor map（key + tensor）
- 輸出: `Q4Schema` + 對應 backend
- 驗收:
  - 偵測 `.absmax/.quant_map` -> `NF4`
  - 偵測 `.qdata/.scale` -> `NVFP4`
  - schema 缺失時回傳可診斷錯誤
- 對應測試: `TC-01`, `TC-02`, `TC-03`

### UC-02 手動覆寫 backend 與 compute path
- 目標: 在除錯/性能評估時可強制 backend 或 fallback。
- 驗收:
  - 可指定 `KernelOnly` / `KernelOrFallback` / `DequantMatmulOnly`
  - 不支援時提供明確錯誤或 fallback 訊息
- 對應測試: `TC-04`, `TC-05`

### UC-03 F# 優先、無 C# 強依賴
- 目標: 公開 API 與主要 interop 流程以 F# 實作。
- 驗收:
  - 專案 API 層純 F#（`.fsi/.fs`）
  - Native interop 先以 F# P/Invoke 方案設計
  - 若遇 runtime 限制，C# 僅作可替換 fallback adapter
- 對應測試: `TC-06`

### UC-04 Unified Memory policy 管理
- 目標: 為 actor 可變 tensor 提供一致的記憶體策略。
- 驗收:
  - 支援 `Disabled` / `PreferUnified` / `RequireUnified`
  - policy 與 backend 能力不相容時給出診斷
- 對應測試: `TC-07`, `TC-08`

### UC-05 建立可重用 Q4 線性層元件
- 目標: 讓 SNN/LLM 都能使用同一套 Q4 linear 封裝。
- 驗收:
  - `Q4Linear` 支援 `PrepareWeight` + `Forward`
  - 輸入 shape/ dtype 不合法時 fail fast
- 對應測試: `TC-09`, `TC-10`

### UC-06 對齊與邊界保護
- 目標: 避免格式/對齊錯誤造成 silent corruption。
- 驗收:
  - kernel 前做必要 alignment/schema validation
  - 產生可追蹤 diagnostics
- 對應測試: `TC-11`, `TC-12`

### UC-07 Actor 併發安全最小保證
- 目標: 多 actor 環境中，至少具備「可推理的更新規約」。
- 驗收:
  - 明確定義 mutable tensor 寫入規約
  - 提供 `session` 級別資源生命週期管理
- 對應測試: `TC-13`

## 3. 非目標 (這一階段)
- 不在此階段實作完整訓練框架。
- 不在此階段保證所有 GPU/driver 組合都可用。
- 不在此階段承諾單一萬用 kernel 覆蓋全部 Q4 格式。
