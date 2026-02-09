import torch
from torchao.prototype.mx_formats.nvfp4_tensor import NVFP4Tensor
x = torch.randn(128, 128, device='cuda', dtype=torch.bfloat16)
t = NVFP4Tensor.to_nvfp4(x)
print(f'Original logical shape: {t.shape}')
print(f'Original qdata shape: {t.qdata.shape}')
t2 = t.t()
print(f'Transposed logical shape: {t2.shape}')
print(f'Transposed qdata shape: {t2.qdata.shape}')
