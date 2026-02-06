#!/bin/bash

# 1. Ensure dotnet is in PATH (Internal SDK location)
export PATH=$PATH:/usr/local/bin/dotnet-sdk

# 2. Define Paths (Allow override via environment variables)
WRAPPER_PATH=${WRAPPER_PATH:-"./TorchSharp/bin/arm64.Release/Native"}
TORCH_LIB_PATH="/usr/local/lib/python3.12/dist-packages/torch/lib"
CUDA_LIB_PATH="/usr/local/cuda/lib64"
APP_DIR=${APP_DIR:-"./TestApp/bin/Release/net8.0"}

# 3. Setup LD_LIBRARY_PATH
# Include current dir (.), the native wrapper, and internal PyTorch/CUDA libs
export LD_LIBRARY_PATH=$APP_DIR:$WRAPPER_PATH:$TORCH_LIB_PATH:$CUDA_LIB_PATH:$LD_LIBRARY_PATH

echo "------------------------------------------"
echo "Using APP_DIR: $APP_DIR"
echo "Using LD_LIBRARY_PATH: $LD_LIBRARY_PATH"
echo "------------------------------------------"

# 4. Execute the application
dotnet $APP_DIR/TestApp.dll