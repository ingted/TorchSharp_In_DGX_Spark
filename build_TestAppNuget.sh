#!/bin/bash
set -e

# 1. Environment Setup
export REPO_ROOT="/workspace/TorchSharp_In_DGX_Spark"
export PATH=$PATH:/usr/local/bin/dotnet-sdk
export DOTNET_ROOT=/usr/local/bin/dotnet-sdk

# 2. Build TestAppNuget
echo "Building TestAppNuget (.NET 8 & 10)..."
cd "$REPO_ROOT/TestAppNuget"
dotnet build -c Release

# 3. Copy Native Library (Required for runtime)
# Even if using NuGet, we might need to manually place the SO if the package structure is incomplete.
NATIVE_SO="$REPO_ROOT/TorchSharp/bin/arm64.Release/Native/libLibTorchSharp.so"

if [ -f "$NATIVE_SO" ]; then
    echo "Copying native library to app bin directories..."
    mkdir -p bin/Release/net8.0/
    mkdir -p bin/Release/net10.0/
    cp "$NATIVE_SO" bin/Release/net8.0/
    cp "$NATIVE_SO" bin/Release/net10.0/
else
    echo "WARNING: libLibTorchSharp.so not found at $NATIVE_SO"
    echo "Please run build_TorchSharp.Native.sh first."
fi

echo "------------------------------------------------"
echo "TestAppNuget build complete."
echo "To run (.NET 8):  APP_DIR="$REPO_ROOT/TestAppNuget/bin/Release/net8.0" bash ../test.sh"
echo "To run (.NET 10): APP_DIR="$REPO_ROOT/TestAppNuget/bin/Release/net10.0" bash ../test.sh"
echo "------------------------------------------------"
