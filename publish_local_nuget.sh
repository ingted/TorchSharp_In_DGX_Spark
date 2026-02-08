#!/bin/bash
set -e

# 1. Environment Setup
export REPO_ROOT="/workspace/TorchSharp_In_DGX_Spark"
export PATH=$PATH:/usr/local/bin/dotnet-sdk
export DOTNET_ROOT=/usr/local/bin/dotnet-sdk

# 2. Pack
echo "Packing FAkka.TorchSharp.DGX..."
cd "$REPO_ROOT/TorchSharp/pkg/TorchSharp"
dotnet pack FAkka.TorchSharp.DGX.nupkgproj -c Release -p:RepoRoot="$REPO_ROOT/TorchSharp/"

# 3. Local "Publish" (Inject into NuGet cache)
VERSION="26.1.0-py3.3"
PACKAGE_NAME="fakka.torchsharp.dgx"
CACHE_DIR="/root/.nuget/packages/$PACKAGE_NAME/$VERSION"
NUPKG="$REPO_ROOT/TorchSharp/bin/packages/Release/FAkka.TorchSharp.DGX.$VERSION.nupkg"

echo "Injecting into local NuGet cache: $CACHE_DIR"

if [ -d "$CACHE_DIR" ]; then
    echo "Removing old cache directory..."
    rm -rf "$CACHE_DIR"
fi

mkdir -p "$CACHE_DIR"

echo "Extracting new package..."
unzip -q "$NUPKG" -d "$CACHE_DIR"

# NuGet also expects the .nupkg and a .sha512 file in the cache directory
# The filename in the cache MUST be lowercase package id
cp "$NUPKG" "$CACHE_DIR/$PACKAGE_NAME.$VERSION.nupkg"
# Create a fake sha512 file if needed (NuGet usually checks this)
sha512sum "$NUPKG" | awk '{print $1}' > "$CACHE_DIR/$PACKAGE_NAME.$VERSION.nupkg.sha512"

echo "------------------------------------------------"
echo "Local publish complete."
echo "You can now rebuild TestAppNuget."
echo "------------------------------------------------"
