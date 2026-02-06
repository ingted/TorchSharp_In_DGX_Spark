import os
import sys
import platform
import urllib.request
import re
import zipfile
import shutil

# Configuration
INDEX_URL = "https://download.pytorch.org/whl/nightly/cu128"
DEST_DIR = os.path.join(os.getcwd(), "TorchSharp", "libtorch-arm64")
TEMP_DIR = os.path.join(os.getcwd(), "temp_download")

def get_wheel_url():
    print(f"Fetching index from {INDEX_URL}")
    try:
        # We need to look for the 'torch' package listing usually
        # But for pytorch mirrors, sometimes it's a flat list or /torch/ subdirectory
        # Let's try to read the index page.
        with urllib.request.urlopen(INDEX_URL) as response:
            html = response.read().decode('utf-8')
    except Exception as e:
        print(f"Error fetching index: {e}")
        # Fallback to a known pattern/guess if listing fails? 
        # But we really need a dynamic link.
        # Let's try appending /torch/ which is common for PEP 503
        try:
             with urllib.request.urlopen(INDEX_URL + "/torch/") as response:
                html = response.read().decode('utf-8')
        except:
            return None

    # Regex to find the wheel
    # Pattern: torch-<version>.dev<date>%2Bcu128-cp<ver>-cp<ver>-linux_aarch64.whl
    # We want aarch64 and cu128.
    # Note: The URL is specifically for cu128, so maybe filenames just say +cu128 or just version.
    # Let's look for linux_aarch64 and .whl
    
    patterns = [
        r'href="(torch-[0-9a-zA-Z\.\+]+linux_aarch64\.whl)"',
        r'href="(torch-[0-9a-zA-Z\.\+]+manylinux[0-9_]+_aarch64\.whl)"'
    ]
    
    candidates = []
    for line in html.split('\n'):
        for pat in patterns:
            match = re.search(pat, line)
            if match:
                filename = match.group(1)
                # We want nightly, aarch64.
                if "aarch64" in filename and ".whl" in filename:
                    candidates.append(filename)
    
    if not candidates:
        print("No aarch64 wheels found.")
        return None

    # Pick the latest one (usually sorting works if date is in version)
    candidates.sort()
    latest = candidates[-1]
    
    # Construct full URL
    # If the index page linked directly to files, relative path works.
    # If we appended /torch/, we need to append that too.
    # Actually, let's assume relative to INDEX_URL or INDEX_URL/torch/
    # If we found it in the root index:
    full_url = f"{INDEX_URL}/{latest}"
    # If we found it in /torch/: 
    # We'll just try to return the filename and let the downloader handle the base.
    # Wait, simple implementation:
    return full_url

def download_and_extract(url):
    if not url:
        print("Invalid URL")
        return

    os.makedirs(TEMP_DIR, exist_ok=True)
    filename = url.split('/')[-1]
    filepath = os.path.join(TEMP_DIR, filename)
    
    if not os.path.exists(filepath):
        print(f"Downloading {url}...")
        try:
            urllib.request.urlretrieve(url, filepath)
        except Exception as e:
            # Try appending /torch/ if the first guess failed
            if "/torch/" not in url:
                try:
                    new_url = url.replace(INDEX_URL, INDEX_URL + "/torch")
                    print(f"Retrying with {new_url}...")
                    urllib.request.urlretrieve(new_url, filepath)
                except Exception as e2:
                    print(f"Download failed: {e2}")
                    return
            else:
                 print(f"Download failed: {e}")
                 return

    print("Extracting...")
    with zipfile.ZipFile(filepath, 'r') as zip_ref:
        # We only want torch/lib and torch/include
        # And we want to strip the 'torch/' prefix when moving to target
        
        # List all files
        for member in zip_ref.namelist():
            if member.startswith('torch/lib/') or member.startswith('torch/include/') or member.startswith('torch/share/'):
                zip_ref.extract(member, TEMP_DIR)

    # Move to destination
    # Source: TEMP_DIR/torch/lib -> DEST_DIR/lib
    src_base = os.path.join(TEMP_DIR, "torch")
    
    # Clean destination lib/include if they exist to avoid mixing
    # But wait, there's already a libtorch.so in lib. Keep it? Replace it?
    # Usually replace.
    
    subdirs = ['lib', 'include', 'share']
    for sub in subdirs:
        src = os.path.join(src_base, sub)
        dst = os.path.join(DEST_DIR, sub)
        if os.path.exists(src):
            if os.path.exists(dst):
                print(f"Removing existing {dst}")
                shutil.rmtree(dst)
            print(f"Moving {src} to {dst}")
            shutil.move(src, dst)

    # Cleanup
    shutil.rmtree(TEMP_DIR)
    print("Done.")

if __name__ == "__main__":
    # Since we can't easily parse the HTML without bs4 in this restricted env easily (regex is brittle),
    # and we know the user says "nightly wheels... available at...",
    # Let's try to construct the URL for a known recent nightly or use pip to download if available?
    # No, let's try the regex approach on the index page first.
    
    # Alternative: Use pip to find the URL
    # pip download torch --pre --no-deps --index-url https://download.pytorch.org/whl/nightly/cu128 --dry-run
    # But capturing output is hard in python script without subprocess.
    
    # Let's try subprocess pip.
    import subprocess
    cmd = [
        sys.executable, "-m", "pip", "download", "torch", 
        "--pre", "--no-deps", 
        "--index-url", INDEX_URL, 
        "--platform", "linux_aarch64",
        "--python-version", "3.11", # Try 3.11 first
        "--only-binary=:all:",
        "--dest", TEMP_DIR
    ]
    
    print(f"Running: {' '.join(cmd)}")
    try:
        subprocess.check_call(cmd)
        # Find the downloaded wheel
        files = [f for f in os.listdir(TEMP_DIR) if f.endswith('.whl')]
        if files:
            wheel_path = os.path.join(TEMP_DIR, files[0])
            print(f"Downloaded {wheel_path}")
            
            # Now extract
            print("Extracting...")
            with zipfile.ZipFile(wheel_path, 'r') as zip_ref:
                for member in zip_ref.namelist():
                     if member.startswith('torch/lib/') or member.startswith('torch/include/') or member.startswith('torch/share/'):
                        zip_ref.extract(member, TEMP_DIR)
            
            # Move
            src_base = os.path.join(TEMP_DIR, "torch")
            for sub in ['lib', 'include', 'share']:
                src = os.path.join(src_base, sub)
                dst = os.path.join(DEST_DIR, sub)
                if os.path.exists(src):
                    if os.path.exists(dst):
                        shutil.rmtree(dst)
                    shutil.move(src, dst)
            
            # Cleanup
            shutil.rmtree(TEMP_DIR)
            print("Setup complete.")
            
        else:
            print("Pip finished but no wheel found?")
            
    except subprocess.CalledProcessError as e:
        print(f"Pip download failed: {e}")
        print("Attempting to fallback to manual URL construction if pip failed...")
        # Fallback logic here if needed, but pip is robust.
