# TorchSharp ARM64 for NVIDIA DGX (Blackwell)

This repository provides a streamlined environment for running **TorchSharp** (the .NET wrapper for LibTorch) on **ARM64** architecture, specifically optimized for **NVIDIA DGX** systems (e.g., Blackwell/GB10) using the official NVIDIA PyTorch containers.

## Background

Standard TorchSharp/LibTorch NuGet packages often lack support for the latest ARM64 + CUDA combinations found on high-performance compute nodes like the DGX. 

To achieve compatibility and performance, this project:
1.  **Native Compilation**: Built the native `libLibTorchSharp.so` wrapper directly on the ARM64 target.
2.  **Container Integration**: Instead of bundling massive LibTorch binaries (multi-GBs), this approach dynamically links to the highly optimized PyTorch libraries already present in the **NVIDIA NGC containers**.
3.  **Slim Deployment**: Removed redundant binaries to keep the deployment footprint minimal (~135MB), relying on `LD_LIBRARY_PATH` to locate system-provided CUDA and PyTorch libs.

## Quick Start (Verification)

To verify the setup within the target environment, use the provided `install_and_run.sh` script. This script installs a temporary .NET runtime and executes the GPU tensor test.

```bash
docker run --rm --gpus all \
  -v .:/workspace \
  -w /workspace \
  nvcr.io/nvidia/pytorch:25.01-py3 \
  ./install_and_run.sh
```

## Key Components

- **TestApp/**: A C# console application demonstrating CUDA availability and basic GPU tensor operations.
- **TorchSharp/**: Contains the core managed DLL and the custom-built ARM64 native wrapper.
- **test.sh**: Orchestrates the `LD_LIBRARY_PATH` to bridge .NET with the container's PyTorch libraries.
- **install_and_run.sh**: A helper for environments without a pre-installed .NET SDK.

## Environment Details
- **Base Image**: `nvcr.io/nvidia/pytorch:25.01-py3`
- **Target Arch**: ARM64 (aarch64)
- **Verified On**: NVIDIA DGX (Blackwell/GB10)
