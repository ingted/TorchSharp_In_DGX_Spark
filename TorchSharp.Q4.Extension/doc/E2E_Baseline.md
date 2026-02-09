# TorchSharp.Q4.Extension E2E Baseline Report

## English Version

### Scope
- Baseline verification for `TorchSharp.Q4.Extension` on branch `26.1.0-py3.1-fp4.0.2`.
- Covers schema detection, backend dispatching, fallback linear path, unified memory policy behavior, and actor-mutable contract baseline.

### Environment
- Date: 2026-02-09
- Runtime: `.NET FSI`
- Package: `FAkka.TorchSharp.DGX 26.1.0-py3.5`
- Script: `TestCase.fsx`

### Commands
```bash
cd /workspace/TorchSharp_In_DGX_Spark_fp4/TorchSharp.Q4.Extension
dotnet build -c Release TorchSharp.Q4.Extension.fsproj
dotnet fsi TestCase.fsx
```

### Result Summary
- Build: `PASS` (0 warning, 0 error)
- Tests: `PASS` (13/13)
- Note: `TC-08` is skip-pass when Unified Memory is available in current environment.

### Baseline Conclusion
- NF4/NVFP4 baseline backend path is available through dequant-matmul fallback.
- `Session -> Q4Linear -> Backend` path is executable.
- Unified memory policy contract and mutable clone policy are in place as baseline.

---

## 中文版

## 範圍
- 驗證 `TorchSharp.Q4.Extension` 在分支 `26.1.0-py3.1-fp4.0.2` 的 baseline 可用性。
- 覆蓋 schema detection、backend dispatch、fallback linear 路徑、Unified Memory policy 行為與 actor-mutable contract baseline。

## 環境
- 日期: 2026-02-09
- Runtime: `.NET FSI`
- 套件: `FAkka.TorchSharp.DGX 26.1.0-py3.5`
- 測試腳本: `TestCase.fsx`

## 指令
```bash
cd /workspace/TorchSharp_In_DGX_Spark_fp4/TorchSharp.Q4.Extension
dotnet build -c Release TorchSharp.Q4.Extension.fsproj
dotnet fsi TestCase.fsx
```

## 結果摘要
- Build: `PASS`（0 warning, 0 error）
- 測試: `PASS`（13/13）
- 備註: `TC-08` 在當前環境支援 Unified Memory 時會走 skip-pass。

## Baseline 結論
- NF4/NVFP4 baseline backend 路徑已可透過 dequant-matmul fallback 執行。
- `Session -> Q4Linear -> Backend` 路徑可執行。
- Unified Memory policy 與 mutable clone 合約已具備 baseline 實作。
