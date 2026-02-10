#!/bin/bash
set -e

# 1. Environment Setup
export REPO_ROOT="/workspace/TorchSharp_In_DGX_Spark_fp4"
export PATH=$PATH:/usr/local/bin/dotnet-sdk

# Ensure we use the local SDK
export DOTNET_ROOT=/usr/local/bin/dotnet-sdk

echo "Building TorchSharp Managed DLLs (.NET 8 & 10)..."

# 2. Build
cd "$REPO_ROOT/TorchSharp/src/TorchSharp"
dotnet build -c Release -p:SkipNative=true

echo "------------------------------------------------"
echo "Managed build complete."
echo "Locations:"
echo " - $REPO_ROOT/TorchSharp/bin/AnyCPU.Release/TorchSharp/net8.0/TorchSharp.dll"
echo " - $REPO_ROOT/TorchSharp/bin/AnyCPU.Release/TorchSharp/net10.0/TorchSharp.dll"
echo "------------------------------------------------"
