#!/bin/bash

# 定義路徑
WRAPPER_PATH="./TorchSharp/bin/arm64.Release/Native"
TORCH_LIB_PATH="/usr/local/lib/python3.12/dist-packages/torch/lib"
CUDA_LIB_PATH="/usr/local/cuda/lib64"
APP_DIR="./TestApp/bin/Release/net8.0"

# 設定環境變數
export LD_LIBRARY_PATH=$APP_DIR:$WRAPPER_PATH:$TORCH_LIB_PATH:$CUDA_LIB_PATH:$LD_LIBRARY_PATH

echo "Using LD_LIBRARY_PATH: $LD_LIBRARY_PATH"

# 執行程式
dotnet $APP_DIR/TestApp.dll
