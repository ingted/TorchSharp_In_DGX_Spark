import torch
from torchao.prototype.mx_formats.nvfp4_tensor import NVFP4Tensor
M, N, K = 9, 128, 128
a_hp = torch.randn(M, K, device='cuda', dtype=torch.bfloat16)
b_hp = torch.randn(N, K, device='cuda', dtype=torch.bfloat16)
a_fp4 = NVFP4Tensor.to_nvfp4(a_hp)
b_fp4 = NVFP4Tensor.to_nvfp4(b_hp)
try:
    qa = a_fp4.qdata.view(torch.float4_e2m1fn_x2)
    qb = b_fp4.qdata.t().view(torch.float4_e2m1fn_x2)
    sa = a_fp4.scale.view(torch.float8_e4m3fn)
    sb = b_fp4.scale.t().contiguous().view(torch.float8_e4m3fn)
    res = torch._scaled_mm(qa, qb, sa, sb, out_dtype=torch.bfloat16)
    print('Success')
    print(res.shape)
except Exception as e:
    print(f'Error: {e}')
