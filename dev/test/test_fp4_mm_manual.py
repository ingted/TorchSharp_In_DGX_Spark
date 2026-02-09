import torch
from torchao.prototype.mx_formats.nvfp4_tensor import NVFP4Tensor
import math

M, N, K = 128, 128, 128
a_hp = torch.randn(M, K, device='cuda', dtype=torch.bfloat16)
b_hp = torch.randn(N, K, device='cuda', dtype=torch.bfloat16)

a_fp4 = NVFP4Tensor.to_nvfp4(a_hp)
b_fp4 = NVFP4Tensor.to_nvfp4(b_hp)

def to_blocked(scale):
    # This is a simplified version of torchao's to_blocked
    # Blackwell FP4 requires scales to be in a specific blocked format for cuBLASLt
    # For 1x16 block size, it's actually just contiguous if we do it right
    return scale.contiguous()

try:
    qa = a_fp4.qdata.view(torch.float4_e2m1fn_x2)
    qb = b_fp4.qdata.view(torch.float4_e2m1fn_x2)
    
    # torchao uses to_blocked for scales
    from torchao.prototype.mx_formats.utils import to_blocked
    sa = to_blocked(a_fp4.scale.view(M, K//16)).view(torch.float8_e4m3fn)
    sb = to_blocked(b_fp4.scale.view(N, K//16)).view(torch.float8_e4m3fn)
    
    res = torch._scaled_mm(
        qa, qb, sa, sb,
        out_dtype=torch.bfloat16
    )
    print('Success')
    print(res.shape)
except Exception as e:
    print(f'Error: {e}')
