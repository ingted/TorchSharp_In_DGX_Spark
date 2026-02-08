#include <ATen/ATen.h>
#include <ATen/cuda/CUDAConfig.h>
#include <ATen/cuda/CUDAContext.h>
#include <c10/cuda/CUDAGuard.h>

extern thread_local char *torch_last_err;

#ifndef kFloat4_e2m1fn_x2
#define kFloat4_e2m1fn_x2 (at::ScalarType)45
#endif
#ifndef kFloat8_e4m3fn
#define kFloat8_e4m3fn (at::ScalarType)24
#endif

extern "C"
{
    __attribute__((visibility("default")))
    void* THSTensor_scaled_mm(void* mat1, void* mat2, void* scale_a, void* scale_b, const int8_t out_dtype)
    {
        try {
            torch_last_err = nullptr;
            at::Tensor a_byte = (*(at::Tensor*)mat1);
            at::Tensor b_byte = (*(at::Tensor*)mat2);
            at::Tensor sa_byte = (*(at::Tensor*)scale_a);
            at::Tensor sb_byte = (*(at::Tensor*)scale_b);
            at::cuda::CUDAGuard guard(a_byte.device());
            
            auto av = at::from_blob(a_byte.data_ptr(), a_byte.sizes(), a_byte.strides(), a_byte.options().dtype(kFloat4_e2m1fn_x2));
            auto bv = at::from_blob(b_byte.data_ptr(), b_byte.sizes(), b_byte.strides(), b_byte.options().dtype(kFloat4_e2m1fn_x2));
            auto sav = at::from_blob(sa_byte.data_ptr(), sa_byte.sizes(), sa_byte.strides(), sa_byte.options().dtype(kFloat8_e4m3fn));
            auto sbv = at::from_blob(sb_byte.data_ptr(), sb_byte.sizes(), sb_byte.strides(), sb_byte.options().dtype(kFloat8_e4m3fn));

            auto st = (c10::ScalarType)out_dtype;
            at::Tensor res = at::_scaled_mm(av, bv, sav, sbv, std::nullopt, std::nullopt, st, false);
            return new at::Tensor(res);
        } catch (const std::exception& e) {
            torch_last_err = strdup(e.what());
            return nullptr;
        }
    }

    __attribute__((visibility("default")))
    void THSFP4_quantize(void* input, void** qdata, void** scale)
    {
        try {
            torch_last_err = nullptr;
            at::Tensor in = (*(at::Tensor*)input).contiguous();
            at::cuda::CUDAGuard guard(in.device());
            int64_t K = in.size(-1);
            int64_t M = in.numel() / K;
            auto in_reshaped = in.reshape({-1, 16});
            auto amax = std::get<0>(at::max(at::abs(in_reshaped), 1));
            auto block_scale = amax / 6.0f;
            auto block_scale_fp8 = at::clamp(block_scale, 1.5258789e-05, 448.0).to(kFloat8_e4m3fn);
            auto block_scale_f32 = block_scale_fp8.to(at::kFloat).unsqueeze(-1);
            auto data_scaled = in_reshaped / (block_scale_f32 + 1e-12f);
            data_scaled = at::clamp(data_scaled, -6.0f, 6.0f);
            auto v = at::abs(data_scaled);
            auto bits = at::zeros(data_scaled.sizes(), data_scaled.options().dtype(at::kByte));
            bits = at::where(v >= 0.25f, 1, bits);
            bits = at::where(v >= 0.75f, 2, bits);
            bits = at::where(v >= 1.25f, 3, bits);
            bits = at::where(v >= 1.75f, 4, bits);
            bits = at::where(v >= 2.50f, 5, bits);
            bits = at::where(v >= 3.50f, 6, bits);
            bits = at::where(v >= 5.00f, 7, bits);
            auto sign = at::where(data_scaled < 0, 8, 0);
            bits = at::bitwise_or(bits, sign);
            auto bits_2d = bits.reshape({M, K});
            auto b1 = bits_2d.index({at::indexing::Slice(), at::indexing::Slice(0, at::indexing::None, 2)});
            auto b2 = bits_2d.index({at::indexing::Slice(), at::indexing::Slice(1, at::indexing::None, 2)});
            auto packed = at::bitwise_or(b1, at::bitwise_left_shift(b2, 4));
            *qdata = new at::Tensor(packed.contiguous());
            *scale = new at::Tensor(block_scale_fp8.reshape({M, K / 16}).contiguous());
        } catch (const std::exception& e) {
            torch_last_err = strdup(e.what());
        }
    }
}