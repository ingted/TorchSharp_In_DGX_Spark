#!/bin/bash
set -e

# 1. Environment Setup
export REPO_ROOT="/workspace/TorchSharp_In_DGX_Spark_fp4"
export TORCH_CMAKE_PATH="/usr/local/lib/python3.12/dist-packages/torch/share/cmake/Torch"

echo "Building Native LibTorchSharp..."
echo "Using PyTorch CMake: $TORCH_CMAKE_PATH"

# 2. Build
cd "$REPO_ROOT/TorchSharp/src/Native"
bash ./build.sh --arch arm64 --configuration Release --libtorchpath "$TORCH_CMAKE_PATH"

echo "------------------------------------------------"
echo "Native build complete."
echo "Artifact: $REPO_ROOT/TorchSharp/bin/arm64.Release/Native/libLibTorchSharp.so"
echo "------------------------------------------------"
