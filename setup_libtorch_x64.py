import os
import sys
import shutil
import zipfile
import urllib.request

# x86_64 setup for testing on local machine
DEST_DIR = os.path.join(os.getcwd(), "TorchSharp", "libtorch-x64")
TEMP_DIR = os.path.join(os.getcwd(), "temp_download_x64")
# Stable CUDA 12.4 LibTorch for Linux x86_64
URL = "https://download.pytorch.org/libtorch/cu124/libtorch-cxx11-abi-shared-with-deps-2.5.1%2Bcu124.zip"

def setup_x64():
    print(f"Setting up LibTorch for x86_64 from {URL}...")
    
    if os.path.exists(DEST_DIR):
        shutil.rmtree(DEST_DIR)
    os.makedirs(TEMP_DIR, exist_ok=True)
    
    filename = "libtorch.zip"
    filepath = os.path.join(TEMP_DIR, filename)
    
    print("Downloading...")
    try:
        urllib.request.urlretrieve(URL, filepath)
        print("Downloaded.")
        
        print("Extracting...")
        with zipfile.ZipFile(filepath, 'r') as zip_ref:
            zip_ref.extractall(TEMP_DIR)
            
        # Move
        # Structure is usually libtorch/lib, libtorch/include
        src_base = os.path.join(TEMP_DIR, "libtorch")
        os.makedirs(DEST_DIR, exist_ok=True)
        
        for sub in ['lib', 'include', 'share']:
            src = os.path.join(src_base, sub)
            dst = os.path.join(DEST_DIR, sub)
            if os.path.exists(src):
                shutil.move(src, dst)
        
        # Cleanup
        shutil.rmtree(TEMP_DIR)
        print("Setup complete for x86_64.")
        
    except Exception as e:
        print(f"Failed: {e}")

if __name__ == "__main__":
    setup_x64()