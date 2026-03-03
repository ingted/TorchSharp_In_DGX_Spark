#!/bin/bash
set -e

# 1. Environment Setup
SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
export REPO_ROOT="${REPO_ROOT:-$SCRIPT_DIR}"
export TORCH_CMAKE_PATH="${TORCH_CMAKE_PATH:-/usr/local/lib/python3.12/dist-packages/torch/share/cmake/Torch}"

echo "Building Native LibTorchSharp..."
echo "Using PyTorch CMake: $TORCH_CMAKE_PATH"

if [[ ! -d "$REPO_ROOT/TorchSharp/src/Native" ]]; then
  echo "Error: Native source directory not found: $REPO_ROOT/TorchSharp/src/Native" >&2
  echo "Hint: export REPO_ROOT=<your-repo-path> before running this script." >&2
  exit 1
fi

# If CLEAN_CMAKE_CACHE=1, remove previous CMake cache to refresh source/binary paths.
if [[ "${CLEAN_CMAKE_CACHE:-0}" == "1" ]]; then
  rm -rf "$REPO_ROOT/TorchSharp/bin/obj/arm64.Release/Native"
fi

# 2. Build
cd "$REPO_ROOT/TorchSharp/src/Native"
bash ./build.sh --arch arm64 --configuration Release --libtorchpath "$TORCH_CMAKE_PATH"

echo "------------------------------------------------"
echo "Native build complete."
echo "Artifact: $REPO_ROOT/TorchSharp/bin/arm64.Release/Native/libLibTorchSharp.so"
echo "------------------------------------------------"
