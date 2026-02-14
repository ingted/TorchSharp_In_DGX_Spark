#!/usr/bin/env bash
set -euo pipefail

# 1. 環境設定
REPO_ROOT="${REPO_ROOT:-/workspace/TorchSharp_In_DGX_Spark_fp4}"
DOTNET_SDK_PATH="${DOTNET_SDK_PATH:-/usr/local/bin/dotnet-sdk}"
export PATH="$PATH:$DOTNET_SDK_PATH"

if [[ -x "$DOTNET_SDK_PATH/dotnet" ]]; then
  DOTNET="$DOTNET_SDK_PATH/dotnet"
else
  DOTNET="dotnet"
fi

PACK_PROJ="$REPO_ROOT/TorchSharp/pkg/pack.proj"
OUT_DIR="$REPO_ROOT/TorchSharp/bin/packages/Release"

echo "開始打包 TorchSharp FP4 NuGet 套件..."
echo "使用專案檔: $PACK_PROJ"
echo "dotnet: $($DOTNET --version)"

# 2. 執行打包
# 傳入 RepoRoot 確保專案能找到對應的二進制檔案
# 關閉 PackageValidation，避免 .NET SDK 10 對 net6.0/netstandard2.0 的 PKV006 阻擋
cd "$REPO_ROOT/TorchSharp/pkg"
"$DOTNET" pack "$PACK_PROJ" -c Release \
  -p:RepoRoot="$REPO_ROOT/TorchSharp/" \
  -p:EnablePackageValidation=false \
  -p:TreatWarningsAsErrors=false \
  -p:WarningsNotAsErrors=NU5128 \
  -p:NoWarn=NU5128 \
  -o "$OUT_DIR"

echo "------------------------------------------------"
echo "打包完成。"
echo "產物位置: $OUT_DIR"
echo "------------------------------------------------"
