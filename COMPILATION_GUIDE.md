# Instruction for Compiling TorchSharp in NVIDIA PyTorch Container

This guide provides steps to compile the native `libLibTorchSharp.so` and the managed `TorchSharp.dll` from source, specifically tailored for the `nvcr.io/nvidia/pytorch:26.01-py3` container environment (Ubuntu 24.04, ARM64, CUDA 13.1).

## 1. Environment Preparation

First, install the necessary build tools and .NET SDKs.

```bash
# Update package list and install dependencies
apt-get update
apt-get install -y libicu-dev cmake build-essential clang wget

# Download and install .NET 8 and .NET 10 SDKs
wget https://dot.net/v1/dotnet-install.sh -O dotnet-install.sh
chmod +x dotnet-install.sh
./dotnet-install.sh --channel 8.0 --install-dir /usr/local/bin/dotnet-sdk
./dotnet-install.sh --channel 10.0 --install-dir /usr/local/bin/dotnet-sdk

# Add dotnet to PATH
export PATH=$PATH:/usr/local/bin/dotnet-sdk
```

## 2. Clone the Repository

```bash
git clone https://github.com/ingted/TorchSharp_In_DGX_Spark.git
cd TorchSharp_In_DGX_Spark
```

## 3. Native Library Compilation (`libLibTorchSharp.so`)

Due to the newer PyTorch version (2.10.0a0) in this container, code adjustments are required in the native source.

### A. Fix `THSAutograd.cpp`
In `TorchSharp/src/Native/LibTorchSharp/THSAutograd.cpp`, update the `_wrap_outputs` call to include the `pure_view` argument (set to `false`).

```cpp
// Around line 179
auto res = torch::autograd::_wrap_outputs(vars, nonDiff, dirty, outputs, node.weak_ptr == nullptr || node.weak_ptr->expired() ? nullptr : node.weak_ptr->lock(), jvp_fn, {}, view_as_self_fn, false);
```

### B. Fix `THSTorch.cpp`
In `TorchSharp/src/Native/LibTorchSharp/THSTorch.cpp`, update the boolean assignment for FP16 reduction.

```cpp
// Around line 56
CATCH(result = (at::globalContext().allowFP16ReductionCuBLAS() == at::CuBLASReductionOption::AllowReducedPrecisionWithSplitK););
```

### C. Run the Build Script
Point the build script to the internal PyTorch installation.

```bash
cd TorchSharp/src/Native
./build.sh --arch arm64 --configuration Release --libtorchpath /usr/local/lib/python3.12/dist-packages/torch/share/cmake/Torch
```

The output library will be located at:
`TorchSharp/bin/arm64.Release/Native/libLibTorchSharp.so`

## 4. Managed Library Compilation (`TorchSharp.dll`)

### A. Update Project Settings
Modify `TorchSharp/src/TorchSharp/TorchSharp.csproj` to include .NET 8 and .NET 10 targets and suppress naming warnings.

1.  Update `<TargetFrameworks>` to: `<TargetFrameworks>net10.0;net8.0;net6.0;netstandard2.0</TargetFrameworks>`
2.  Add `<NoWarn>$(NoWarn);CS8981</NoWarn>` to the main `<PropertyGroup>`.

Also, update `TorchSharp/global.json` to allow the newer SDKs:
```json
{
  "sdk": {
    "version": "8.0.100",
    "rollForward": "latestMajor",
    "allowPrerelease": true
  }
}
```

### B. Build the DLLs
Run the dotnet build command skipping the native redistribution logic.

```bash
cd ../TorchSharp
dotnet build -c Release -p:SkipNative=true
```

## 5. TestApp Compilation

### A. Update Project Settings
Modify `TestApp/TestApp.csproj` to target both .NET 8 and .NET 10.

```xml
<TargetFrameworks>net10.0;net8.0</TargetFrameworks>
```

### B. Build and Run
```bash
cd TestApp
dotnet build -c Release -p:SkipNative=true

# Copy the native library to the output directory
cp ../TorchSharp/bin/arm64.Release/Native/libLibTorchSharp.so bin/Release/net8.0/
cp ../TorchSharp/bin/arm64.Release/Native/libLibTorchSharp.so bin/Release/net10.0/

# Run .NET 8 version
export LD_LIBRARY_PATH=.:/usr/local/lib/python3.12/dist-packages/torch/lib:/usr/local/cuda/lib64:$LD_LIBRARY_PATH
cd bin/Release/net8.0/
dotnet TestApp.dll

# Run .NET 10 version
cd ../net10.0/
dotnet TestApp.dll
```

## 6. Using `test.sh`

The provided `test.sh` script automates the environment variable setup and execution. To run it correctly, ensure `dotnet` is in your `PATH`.

```bash
# Add dotnet to PATH if not already done
export PATH=$PATH:/usr/local/bin/dotnet-sdk

# Execute from the repository root
bash test.sh
```

To test the **.NET 10** version using the script, you can either modify `APP_DIR` in `test.sh` or run:
```bash
APP_DIR="./TestApp/bin/Release/net10.0" bash test.sh
```

## 7. Summary of Build Artifacts

*   **Native Library:**
    *   `/workspace/TorchSharp_In_DGX_Spark/TorchSharp/bin/arm64.Release/Native/libLibTorchSharp.so`
*   **Managed Libraries:**
    *   **[.NET 8]:** `/workspace/TorchSharp_In_DGX_Spark/TorchSharp/bin/AnyCPU.Release/TorchSharp/net8.0/TorchSharp.dll`
    *   **[.NET 10]:** `/workspace/TorchSharp_In_DGX_Spark/TorchSharp/bin/AnyCPU.Release/TorchSharp/net10.0/TorchSharp.dll`
