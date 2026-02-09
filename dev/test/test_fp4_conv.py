import torch
import sys
try:
    x = torch.randn(1024, 1024, device='cuda', dtype=torch.float16)
    y = x.to(torch.float4_e2m1fn_x2)
    print('Success')
except Exception as e:
    print(f'Error: {e}')
    sys.exit(1)
