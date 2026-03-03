#!/bin/bash
set -euo pipefail

# 1. Ensure dotnet is in PATH (Internal SDK location)
export PATH=$PATH:/usr/local/bin/dotnet-sdk

# 2. Define Paths (Allow override via environment variables)
SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="${REPO_ROOT:-$SCRIPT_DIR}"
WRAPPER_PATH=${WRAPPER_PATH:-"$REPO_ROOT/TorchSharp/bin/arm64.Release/Native"}
TORCH_LIB_PATH="/usr/local/lib/python3.12/dist-packages/torch/lib"
CUDA_LIB_PATH="/usr/local/cuda/lib64"

if [[ -z "${APP_DIR:-}" ]]; then
  for candidate in \
    "$REPO_ROOT/TestApp/bin/Release/net8.0/linux-arm64" \
    "$REPO_ROOT/TestApp/bin/Release/net10.0/linux-arm64" \
    "$REPO_ROOT/TestApp/bin/Release/net8.0" \
    "$REPO_ROOT/TestApp/bin/Release/net10.0"; do
    if [[ -f "$candidate/TestApp.dll" ]]; then
      APP_DIR="$candidate"
      break
    fi
  done
fi

if [[ -z "${APP_DIR:-}" || ! -f "$APP_DIR/TestApp.dll" ]]; then
  echo "Error: TestApp.dll not found." >&2
  echo "Please build TestApp first: bash \"$REPO_ROOT/build_TestApp.sh\"" >&2
  echo "Or set APP_DIR explicitly to a folder containing TestApp.dll." >&2
  exit 1
fi

# 3. Setup LD_LIBRARY_PATH
# Include current dir (.), the native wrapper, and internal PyTorch/CUDA libs
export LD_LIBRARY_PATH=$APP_DIR:$WRAPPER_PATH:$TORCH_LIB_PATH:$CUDA_LIB_PATH:$LD_LIBRARY_PATH

echo "------------------------------------------"
echo "Using APP_DIR: $APP_DIR"
echo "Using LD_LIBRARY_PATH: $LD_LIBRARY_PATH"
echo "------------------------------------------"

# 4. Execute the application
dotnet $APP_DIR/TestApp.dll
