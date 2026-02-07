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

# 3. Native Library handling
# The FAkka.TorchSharp.DGX NuGet package now includes libLibTorchSharp.so
# and will automatically deploy it to the output directory.
echo "Native library handled by NuGet package."

echo "------------------------------------------------"
echo "TestAppNuget build complete."
echo "To run (.NET 8):  APP_DIR=\"$REPO_ROOT/TestAppNuget/bin/Release/net8.0/linux-arm64\" bash test.sh"
echo "To run (.NET 10): APP_DIR=\"$REPO_ROOT/TestAppNuget/bin/Release/net10.0/linux-arm64\" bash test.sh"
echo "------------------------------------------------"
