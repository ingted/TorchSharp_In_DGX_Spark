import torch
from torchao.prototype.mx_formats.nvfp4_tensor import NVFP4Tensor
M, N, K = 128, 128, 128
a_hp = torch.randn(M, K, device='cuda', dtype=torch.bfloat16)
b_hp = torch.randn(K, N, device='cuda', dtype=torch.bfloat16)

# NVFP4Tensor only supports linear-like (weight is transposed)
# So we quantize b_hp.t() as the weight
weight_fp4 = NVFP4Tensor.to_nvfp4(b_hp.t().contiguous())
# and activate dynamic quantization for the input
# (or just use NVFP4Tensor for input too if we want to manually call it)
input_fp4 = NVFP4Tensor.to_nvfp4(a_hp)

try:
    # This should trigger the overloaded mm/linear in torchao
    # which eventually calls _scaled_mm
    # We need to set act_quant_kwargs to trigger the scaled_mm path
    from torchao.prototype.mx_formats.nvfp4_tensor import QuantizeTensorToNVFP4Kwargs
    weight_fp4.act_quant_kwargs = QuantizeTensorToNVFP4Kwargs(use_triton_kernel=False)
    
    res = torch.mm(input_fp4, weight_fp4.t())
    print('Success')
    print(res.shape)
except Exception as e:
    print(f'Error: {e}')
