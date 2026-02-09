import torch
from torchao.prototype.mx_formats.nvfp4_tensor import NVFP4Tensor
x = torch.randn(1024, 1024, device='cuda', dtype=torch.bfloat16)
try:
    x_nvfp4 = NVFP4Tensor.to_nvfp4(x)
    print('Success')
    print(f'qdata shape: {x_nvfp4.qdata.shape}, dtype: {x_nvfp4.qdata.dtype}')
    print(f'scale shape: {x_nvfp4.scale.shape}, dtype: {x_nvfp4.scale.dtype}')
except Exception as e:
    print(f'Error: {e}')
