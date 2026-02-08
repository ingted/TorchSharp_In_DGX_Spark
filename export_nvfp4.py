import leb128
import torch
import numpy as np

def _elem_type(t):
    dt = t.dtype

    if dt == torch.uint8:
        return 0
    elif dt == torch.int8:
        return 1
    elif dt == torch.int16:
        return 2
    elif dt == torch.int32:
        return 3
    elif dt == torch.int64:
        return 4
    elif dt == torch.float16:
        return 5
    elif dt == torch.float32:
        return 6
    elif dt == torch.float64:
        return 7
    elif dt == torch.bool:
        return 11
    elif dt == torch.bfloat16:
        return 15
    elif dt == torch.float8_e4m3fn:
        # Custom code for FP8 E4M3
        return 101
    elif dt == torch.float4_e2m1fn_x2:
        # Custom code for NVFP4 x2
        return 100
    else:
        return 4711

def _write_tensor(t, stream):
    etype = _elem_type(t)
    stream.write(leb128.u.encode(etype))
    
    # For float4_e2m1fn_x2, we store the logical shape
    # But the raw data will be half the size in the last dimension
    stream.write(leb128.u.encode(len(t.shape)))
    for s in t.shape:
        stream.write(leb128.u.encode(s))
    
    if t.dtype == torch.float4_e2m1fn_x2 or t.dtype == torch.float8_e4m3fn:
        # view as uint8 to get packed bytes (numpy doesn't support these dtypes)
        raw_bytes = t.view(torch.uint8).detach().cpu().numpy().tobytes()
    else:
        raw_bytes = t.detach().cpu().numpy().tobytes()
        
    stream.write(raw_bytes)

def save_state_dict(sd, stream):
    stream.write(leb128.u.encode(len(sd)))
    for entry in sd:
        stream.write(leb128.u.encode(len(entry)))
        stream.write(bytes(entry, 'utf-8'))
        _write_tensor(sd[entry], stream)
