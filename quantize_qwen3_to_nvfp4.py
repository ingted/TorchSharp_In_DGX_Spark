import torch
import os
import sys
from dat_reader import load_state_dict
import export_nvfp4 as exportsd
from torchao.prototype.mx_formats.nvfp4_tensor import NVFP4Tensor

def is_quantizable(name, tensor):
    if len(tensor.shape) != 2:
        return False
    if "embed_tokens" in name:
        return False
    if "norm" in name:
        return False
    # Quantize projections
    quant_names = ["q_proj", "k_proj", "v_proj", "o_proj", "gate_proj", "up_proj", "down_proj", "lm_head"]
    return any(qn in name for qn in quant_names)

def main():
    input_path = "/models/qwen3-4b-instruct-2507-torchsharp/Qwen3-4B-Instruct-2507-fp16.dat"
    output_path = "/models/qwen3-4b-instruct-2507-torchsharp/Qwen3-4B-Instruct-2507-nvfp4.dat"
    
    print(f"Loading {input_path}...")
    sd = load_state_dict(input_path)
    
    new_sd = {}
    total = len(sd)
    for i, (name, t) in enumerate(sd.items()):
        print(f"[{i+1}/{total}] Processing {name}...")
        
        if is_quantizable(name, t):
            print(f"  Quantizing {name} to NVFP4...")
            # Ensure it's on CUDA for torchao conversion
            t_cuda = t.to("cuda").to(torch.bfloat16)
            
            # Pad K dimension to be multiple of 16 if needed
            # (Qwen3-4B hidden size is 2560, which is 16 * 160, so no padding usually needed)
            if t_cuda.shape[1] % 16 != 0:
                print(f"  Warning: Padding {name} K dimension from {t_cuda.shape[1]} to next multiple of 16")
                pad = 16 - (t_cuda.shape[1] % 16)
                t_cuda = torch.nn.functional.pad(t_cuda, (0, pad))
            
            x_nvfp4 = NVFP4Tensor.to_nvfp4(t_cuda)
            
            # Store qdata and scale separately in the new state dict
            # We append suffixes so the loader can find them
            new_sd[name + ".qdata"] = x_nvfp4.qdata
            new_sd[name + ".scale"] = x_nvfp4.scale
            
            # Optional: store original logical shape if we want to be exact
            # But qdata.shape[0] and scale.shape[1]*16 give us M and K.
        else:
            new_sd[name] = t
            
    print(f"Saving to {output_path}...")
    with open(output_path, "wb") as f:
        exportsd.save_state_dict(new_sd, f)
    print("Done!")

if __name__ == "__main__":
    main()
