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

## Recompilation

Since this repository includes the source code under `TorchSharp/src/Native`, you can recompile the native wrapper (`libLibTorchSharp.so`) directly within the NVIDIA container. This ensures the wrapper is perfectly matched to the container's PyTorch version.

### Steps to Rebuild Native Wrapper:

1. **Start the Container**:
   ```bash
   docker run --rm -it --gpus all -v .:/workspace -w /workspace nvcr.io/nvidia/pytorch:25.01-py3 bash
   ```

2. **Run Build Script**:
   Inside the container, execute the following:
   ```bash
   cd TorchSharp/src/Native
   bash build.sh --arch arm64 --configuration Release --libtorchpath /usr/local/lib/python3.12/dist-packages/torch/share/cmake/Torch
   ```

The resulting binary will be placed in `TorchSharp/bin/arm64.Release/Native/libLibTorchSharp.so`.

### Requirements for Build:
- The build script uses **CMake** and **g++**, both of which are pre-installed in the `nvcr.io/nvidia/pytorch` image.
- The `--libtorchpath` argument points to the internal PyTorch installation inside the container, eliminating the need for external LibTorch downloads.

---

# TorchSharp ARM64 (針對 NVIDIA DGX Blackwell 最佳化) - 中文說明

本倉庫提供了一個精簡的環境，用於在 **ARM64** 架構（特別是配備 Blackwell/GB10 GPU 的 **NVIDIA DGX** 系統）上執行 **TorchSharp** (.NET 版 LibTorch 封裝)。

## 專案背景

標準的 TorchSharp/LibTorch NuGet 套件通常缺乏對最新 ARM64 + CUDA 組合（如 DGX 節點）的完整支援。

為了兼顧相容性與效能，本專案採用以下策略：
1. **原生編譯**：直接在 ARM64 目標機器上編譯 `libLibTorchSharp.so`。
2. **容器整合**：捨棄動輒數 GB 的 LibTorch 二進位包，改為動態連結 **NVIDIA NGC 容器**內建且經過高度最佳化的 PyTorch 庫。
3. **極致精簡**：移除所有冗餘檔案，將部署包體積壓縮至約 135MB。

## 快速開始 (驗證環境)

請在目標機器執行以下指令。此指令會啟動容器、安裝臨時的 .NET Runtime 並執行 GPU Tensor 運算測試。

```bash
docker run --rm --gpus all \
  -v .:/workspace \
  -w /workspace \
  nvcr.io/nvidia/pytorch:25.01-py3 \
  ./install_and_run.sh
```

## 重新編譯 Native Wrapper

若需針對不同的容器版本重新編譯：

1. **進入容器**:
   ```bash
   docker run --rm -it --gpus all -v .:/workspace -w /workspace nvcr.io/nvidia/pytorch:25.01-py3 bash
   ```

2. **執行編譯腳本**:
   ```bash
   cd TorchSharp/src/Native
   bash build.sh --arch arm64 --configuration Release --libtorchpath /usr/local/lib/python3.12/dist-packages/torch/share/cmake/Torch
   ```

編譯產物將位於 `TorchSharp/bin/arm64.Release/Native/libLibTorchSharp.so`。
