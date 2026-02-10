#!/bin/bash
set -e

# 1. 環境設定
export REPO_ROOT="/workspace/TorchSharp_In_DGX_Spark_fp4"
export DOTNET_SDK_PATH="/usr/local/bin/dotnet-sdk"
export PATH=$PATH:$DOTNET_SDK_PATH

echo "開始打包 TorchSharp FP4 NuGet 套件..."
echo "使用專案檔: $REPO_ROOT/TorchSharp/pkg/pack.proj"

# 2. 執行打包
# 傳入 RepoRoot 確保專案能找到對應的二進制檔案
# 輸出路徑設定在 bin/packages/Release
cd "$REPO_ROOT/TorchSharp/pkg"
dotnet pack pack.proj -c Release 
    -p:RepoRoot="$REPO_ROOT/TorchSharp/" 
    -o "$REPO_ROOT/TorchSharp/bin/packages/Release"

echo "------------------------------------------------"
echo "打包完成。"
echo "產物位置: $REPO_ROOT/TorchSharp/bin/packages/Release"
echo "------------------------------------------------"
