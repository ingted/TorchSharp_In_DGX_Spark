import torch
from torchao.prototype.mx_formats.nvfp4_tensor import NVFP4Tensor
from torchao.prototype.mx_formats.utils import to_blocked
M, N, K = 128, 128, 128
a_hp = torch.randn(M, K, device='cuda', dtype=torch.bfloat16)
b_hp = torch.randn(N, K, device='cuda', dtype=torch.bfloat16)
a_fp4 = NVFP4Tensor.to_nvfp4(a_hp)
b_fp4 = NVFP4Tensor.to_nvfp4(b_hp)
try:
    qa = a_fp4.qdata.view(torch.float4_e2m1fn_x2)
    # Passing the transpose of b_fp4.qdata
    qb = b_fp4.qdata.t().view(torch.float4_e2m1fn_x2)
    
    sa = to_blocked(a_fp4.scale.view(M, K//16)).view(torch.float8_e4m3fn)
    sb = to_blocked(b_fp4.scale.view(N, K//16)).view(torch.float8_e4m3fn)
    
    # sa is [M, K//16] blocked
    # sb is [N, K//16] blocked but we need it transposed for the call if b is transposed?
    # Actually torchao says: b_scale_blocked = to_blocked(b.scale.t().view(N, K // b.block_size))
    # Wait, if b is weight.t(), then b.scale is weight.scale.t()
    
    res = torch._scaled_mm(
        qa, qb, sa, sb,
        out_dtype=torch.bfloat16
    )
    print('Success')
    print(res.shape)
except Exception as e:
    print(f'Error: {e}')
