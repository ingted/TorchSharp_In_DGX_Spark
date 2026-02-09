import torch
from torchao.prototype.mx_formats.nvfp4_tensor import NVFP4Tensor
x = torch.randn(128, 128, device='cuda', dtype=torch.bfloat16)
t = NVFP4Tensor.to_nvfp4(x)
t2 = t.t()
print(f'qdata contiguous? {t2.qdata.is_contiguous()}')
print(f'scale contiguous? {t2.scale.is_contiguous()}')
