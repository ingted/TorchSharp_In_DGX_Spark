import leb128
import torch
import numpy as np
import io

def read_leb128(stream):
    return leb128.u.decode_reader(stream)[0]

def _get_torch_dtype(type_code):
    mapping = {
        0: torch.uint8,
        1: torch.int8,
        2: torch.int16,
        3: torch.int32,
        4: torch.int64,
        5: torch.float16,
        6: torch.float32,
        7: torch.float64,
        11: torch.bool,
        15: torch.bfloat16
    }
    return mapping.get(type_code, None)

def read_tensor(stream):
    type_code = read_leb128(stream)
    dtype = _get_torch_dtype(type_code)
    
    num_dims = read_leb128(stream)
    shape = []
    for _ in range(num_dims):
        shape.append(read_leb128(stream))
    
    num_elements = 1
    for s in shape:
        num_elements *= s
        
    if dtype == torch.float16:
        # np.float16 is compatible with torch.float16
        np_dtype = np.float16
        element_size = 2
    elif dtype == torch.bfloat16:
        # numpy doesn't have bfloat16, use uint16
        np_dtype = np.uint16
        element_size = 2
    elif dtype == torch.float32:
        np_dtype = np.float32
        element_size = 4
    elif dtype == torch.float64:
        np_dtype = np.float64
        element_size = 8
    elif dtype == torch.uint8:
        np_dtype = np.uint8
        element_size = 1
    elif dtype == torch.int8:
        np_dtype = np.int8
        element_size = 1
    elif dtype == torch.int32:
        np_dtype = np.int32
        element_size = 4
    elif dtype == torch.int64:
        np_dtype = np.int64
        element_size = 8
    else:
        np_dtype = np.uint8
        element_size = 1

    total_bytes = num_elements * element_size
    raw_data = stream.read(total_bytes)
    
    if dtype:
        arr = np.frombuffer(raw_data, dtype=np_dtype).copy()
        t = torch.from_numpy(arr)
        if dtype == torch.bfloat16:
            t = t.view(torch.bfloat16)
        return t.reshape(shape)
    else:
        return raw_data

def load_state_dict(file_path):
    with open(file_path, "rb") as f:
        num_entries = read_leb128(f)
        sd = {}
        for _ in range(num_entries):
            name_len = read_leb128(f)
            name = f.read(name_len).decode('utf-8')
            tensor = read_tensor(f)
            sd[name] = tensor
        return sd
