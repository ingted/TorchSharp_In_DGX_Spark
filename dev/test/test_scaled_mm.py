import torch
M, N, K = 128, 128, 128
a = torch.randn(M, K, device='cuda', dtype=torch.bfloat16).to(torch.float8_e4m3fn)
# b must be KxN and typically we want it to be column-major or we pass it transposed
b = torch.randn(N, K, device='cuda', dtype=torch.bfloat16).to(torch.float8_e4m3fn).t()
a_scale = torch.tensor([1.0], device='cuda', dtype=torch.float32)
b_scale = torch.tensor([1.0], device='cuda', dtype=torch.float32)
try:
    res = torch._scaled_mm(a, b, a_scale, b_scale, out_dtype=torch.bfloat16)
    print('Success')
    print(res.shape)
except Exception as e:
    print(f'Error: {e}')
