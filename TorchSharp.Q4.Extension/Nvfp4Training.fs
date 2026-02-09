namespace TorchSharp.Q4.Extension

open System
open TorchSharp

module Nvfp4TrainingImpl =
  let isFloatingDtype (dtype: TorchSharp.torch.ScalarType) =
    dtype = torch.float16
    || dtype = torch.float32
    || dtype = torch.float64
    || dtype = torch.bfloat16

  let ensureMatrix (name: string) (tensor: TorchSharp.torch.Tensor) =
    if tensor.shape.Length <> 2 then
      raise (InvalidOperationException(sprintf "%s must be rank-2, got rank=%d." name tensor.shape.Length))
    tensor

  let ensureKAligned (name: string) (k: int64) =
    if k % 16L <> 0L then
      raise (InvalidOperationException(sprintf "%s requires in_features divisible by 16, got %d." name k))

  let nvfp4Codebook (device: TorchSharp.torch.Device) =
    let values =
      [|
        0.0f; 0.5f; 1.0f; 1.5f
        2.0f; 3.0f; 4.0f; 6.0f
        -0.0f; -0.5f; -1.0f; -1.5f
        -2.0f; -3.0f; -4.0f; -6.0f
      |]
    torch.tensor(values, dtype = torch.float32).``to``(device)

  let decodePacked (packed: TorchSharp.torch.Tensor) =
    let q = ensureMatrix "qdata" packed
    let qU8 = q.to_type(torch.uint8)
    let lowMask = torch.tensor(15uy, dtype = torch.uint8, device = q.device)
    let shift4 = torch.tensor(4uy, dtype = torch.uint8, device = q.device)
    let low = torch.bitwise_and(qU8, lowMask)
    let high = torch.bitwise_right_shift(qU8, shift4)
    low, high

  let decodeToIndices (packed: TorchSharp.torch.Tensor) =
    let low, high = decodePacked packed
    use stacked = torch.stack([| low; high |], dim = 2L)
    stacked.to_type(torch.int64)

  let fallbackQuantizePacked (input: TorchSharp.torch.Tensor) =
    let x2d = ensureMatrix "input" input
    let outFeatures = x2d.shape.[0]
    let inFeatures = x2d.shape.[1]
    ensureKAligned "NVFP4 quantize fallback" inFeatures

    let x32 = if x2d.dtype = torch.float32 then x2d else x2d.to_type(torch.float32)
    use x3d = x32.reshape([| outFeatures; inFeatures / 16L; 16L |])
    use absmax = x3d.abs().amax([| 2L |], keepdim = true)
    use eps = torch.tensor(1e-6f, dtype = torch.float32, device = x2d.device)
    use scale = torch.maximum(absmax / 6.0, eps)
    use normalized = x3d / scale
    use codebook = nvfp4Codebook x2d.device
    use diff = (normalized.unsqueeze(-1L) - codebook).abs()
    use idx = diff.argmin(-1L).to_type(torch.uint8)

    use idxPair = idx.reshape([| outFeatures; inFeatures / 2L; 2L |])
    use low = idxPair.narrow(2L, 0L, 1L).squeeze(2L).to_type(torch.int16)
    use high = idxPair.narrow(2L, 1L, 1L).squeeze(2L).to_type(torch.int16)
    use packedI16 = low + high * 16
    let packed = packedI16.to_type(torch.uint8)
    let scale2d = scale.squeeze(-1L).contiguous().to_type(torch.float16)
    packed, scale2d

module Nvfp4Training =
  let quantizePacked (input: TorchSharp.torch.Tensor) =
    let x2d = Nvfp4TrainingImpl.ensureMatrix "input" input
    let inFeatures = x2d.shape.[1]
    Nvfp4TrainingImpl.ensureKAligned "NVFP4 quantize" inFeatures

    let useNative =
      x2d.device_type = DeviceType.CUDA
      && NativeInterop.hasLibTorchFp4Quantize()

    if useNative then
      NativeInterop.fp4Quantize x2d
    else
      Nvfp4TrainingImpl.fallbackQuantizePacked x2d

  let dequantizePacked
    (qdata: TorchSharp.torch.Tensor)
    (scale: TorchSharp.torch.Tensor)
    (outDtype: TorchSharp.torch.ScalarType)
    =
    let q2d = Nvfp4TrainingImpl.ensureMatrix "qdata" qdata
    let s2d = Nvfp4TrainingImpl.ensureMatrix "scale" scale
    let outFeatures = q2d.shape.[0]
    let inFeatures = q2d.shape.[1] * 2L
    Nvfp4TrainingImpl.ensureKAligned "NVFP4 dequantize" inFeatures

    let expectedScaleCols = inFeatures / 16L
    if s2d.shape.[0] <> outFeatures || s2d.shape.[1] <> expectedScaleCols then
      raise (
        InvalidOperationException(
          sprintf
            "scale shape mismatch: expected [%d,%d], got [%d,%d]."
            outFeatures
            expectedScaleCols
            s2d.shape.[0]
            s2d.shape.[1]
        )
      )

    use idx = Nvfp4TrainingImpl.decodeToIndices q2d
    use flatIdx = idx.reshape(-1L)
    use codebook = Nvfp4TrainingImpl.nvfp4Codebook q2d.device
    use flatVals = torch.index_select(codebook, 0L, flatIdx)
    use vals = flatVals.reshape([| outFeatures; inFeatures / 16L; 16L |])

    let scaleForApply =
      if s2d.dtype = torch.float32 then s2d else s2d.to_type(torch.float32)

    let scaled = vals * scaleForApply.unsqueeze(-1L)
    let dense32 = scaled.reshape([| outFeatures; inFeatures |]).contiguous()
    let dense =
      if outDtype = torch.float32 then dense32 else dense32.to_type(outDtype)
    dense.contiguous().clone()

  let steWeight (masterWeight: TorchSharp.torch.Tensor) =
    let w2d = Nvfp4TrainingImpl.ensureMatrix "masterWeight" masterWeight
    if not (Nvfp4TrainingImpl.isFloatingDtype w2d.dtype) then
      raise (InvalidOperationException(sprintf "masterWeight must be floating dtype, got %A." w2d.dtype))

    let q, s = quantizePacked w2d
    use qd = q
    use sd = s
    use dq = dequantizePacked qd sd w2d.dtype
    w2d + (dq - w2d).detach()

  let linearSte
    (input: TorchSharp.torch.Tensor)
    (masterWeight: TorchSharp.torch.Tensor)
    (outDtype: TorchSharp.torch.ScalarType)
    =
    let inFeatures = input.shape.[input.shape.Length - 1]
    let w = Nvfp4TrainingImpl.ensureMatrix "masterWeight" masterWeight
    if w.shape.[1] <> inFeatures then
      raise (
        InvalidOperationException(
          sprintf "Input feature size mismatch: input=%d, weight.in=%d." inFeatures w.shape.[1]
        )
      )

    use wSte = steWeight w
    let computeInput =
      if input.dtype = wSte.dtype then input else input.to_type(wSte.dtype)
    let output = torch.nn.functional.linear(computeInput, wSte)
    if output.dtype = outDtype then output else output.to_type(outDtype)
