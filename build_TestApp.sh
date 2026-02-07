#!/bin/bash
set -e

# 1. Environment Setup
export REPO_ROOT="/workspace/TorchSharp_In_DGX_Spark"
export PATH=$PATH:/usr/local/bin/dotnet-sdk
export DOTNET_ROOT=/usr/local/bin/dotnet-sdk

# 2. Build TestApp
echo "Building TestApp (.NET 8 & 10)..."
cd "$REPO_ROOT/TestApp"
dotnet build -c Release -p:SkipNative=true

# 3. Copy Native Library (Required for runtime)
NATIVE_SO="$REPO_ROOT/TorchSharp/bin/arm64.Release/Native/libLibTorchSharp.so"

if [ -f "$NATIVE_SO" ]; then
    echo "Copying native library to app bin directories..."
    cp "$NATIVE_SO" bin/Release/net8.0/
    cp "$NATIVE_SO" bin/Release/net10.0/
else
    echo "WARNING: libLibTorchSharp.so not found at $NATIVE_SO"
    echo "Please run build_TorchSharp.Native.sh first."
fi

echo "------------------------------------------------"
echo "TestApp build complete."
echo "To run (.NET 8):  bash ../test.sh"
echo "To run (.NET 10): APP_DIR="$REPO_ROOT/TestApp/bin/Release/net10.0" bash ../test.sh"
echo "------------------------------------------------"
