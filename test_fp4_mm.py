import torch
from torchao.prototype.mx_formats.nvfp4_tensor import NVFP4Tensor
M, N, K = 128, 128, 128
a_hp = torch.randn(M, K, device='cuda', dtype=torch.bfloat16)
# weights are N x K, we want K x N for mm (row-major input, column-major weight)
# or we pass N x K transposed
b_hp = torch.randn(N, K, device='cuda', dtype=torch.bfloat16)

a_fp4 = NVFP4Tensor.to_nvfp4(a_hp)
# Quantize b_hp.t() to get contiguous KxN fp4 weight
b_fp4_t = NVFP4Tensor.to_nvfp4(b_hp.t().contiguous())

try:
    qa = a_fp4.qdata.view(torch.float4_e2m1fn_x2)
    qb = b_fp4_t.qdata.view(torch.float4_e2m1fn_x2)
    sa = a_fp4.scale.view(torch.float8_e4m3fn)
    sb = b_fp4_t.scale.view(torch.float8_e4m3fn)
    
    # sa and sb must have 1024 elements total for 128x128 matrix with block size 16
    # sa shape: [128, 8], sb shape: [8, 128]
    
    res = torch._scaled_mm(
        qa, qb, sa, sb,
        out_dtype=torch.bfloat16
    )
    print('Success')
    print(f'qa data_ptr: {qa.data_ptr()}')
    print(f'qb data_ptr: {qb.data_ptr()}')
    print(res.shape)
except Exception as e:
    print(f'Error: {e}')
