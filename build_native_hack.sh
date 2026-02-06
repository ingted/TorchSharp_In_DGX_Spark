#!/bin/bash
set -e

SYSTEM_LIB="/usr/lib/x86_64-linux-gnu/libcudart.so"
BACKUP_LIB="${SYSTEM_LIB}.bak"
DUMMY_LIB="/torchsharp_arm64/TorchSharp/libtorch-arm64/lib/libcudart.so"

echo "Backing up system libcudart.so..."
if [ -L "$SYSTEM_LIB" ]; then
    sudo mv "$SYSTEM_LIB" "$BACKUP_LIB"
    echo "Backup created."
else
    echo "Error: $SYSTEM_LIB is not a symlink or doesn't exist. Aborting."
    exit 1
fi

echo "Symlinking dummy ARM64 lib..."
sudo ln -s "$DUMMY_LIB" "$SYSTEM_LIB"

echo "Running build..."
export PATH=$PWD/cmake-3.29.0-linux-x86_64/bin:$PATH
export CC=aarch64-linux-gnu-gcc
export CXX=aarch64-linux-gnu-g++
export QEMU_LD_PREFIX=/usr/aarch64-linux-gnu

# Run the build logic manually to catch errors but proceed to cleanup
if bash TorchSharp/src/Native/build.sh --arch arm64 --configuration Release --libtorchpath $(pwd)/TorchSharp/libtorch-arm64/share/cmake/Torch; then
    echo "Build SUCCESS!"
else
    echo "Build FAILED!"
fi

echo "Restoring system lib..."
sudo rm "$SYSTEM_LIB"
sudo mv "$BACKUP_LIB" "$SYSTEM_LIB"
echo "Done."
