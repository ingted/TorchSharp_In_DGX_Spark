#if INTERACTIVE
#r "nuget: FAkka.TorchSharp.DGX, 26.1.0-py3.6"
#r "./bin/Release/net10.0/TorchSharp.Q4.Extension.dll"
#endif

open System
open TorchSharp
open TorchSharp.Q4.Extension

let ensure cond msg =
  if not cond then
    failwith msg

let expectThrows (f: unit -> unit) =
  try
    f ()
    false
  with _ ->
    true

let canRunFp4Kernel () =
  torch.cuda_is_available()
  && NativeInterop.hasLibTorchFp4Quantize()
  && NativeInterop.hasLibTorchScaledMm()

let toBlocked (input: torch.Tensor) =
  use _ = torch.NewDisposeScope()
  let rows = input.shape.[0]
  let cols = input.shape.[1]
  let rowBlocks = (rows + 127L) / 128L
  let colBlocks = (cols + 3L) / 4L
  use padded = torch.zeros([| rowBlocks * 128L; colBlocks * 4L |], input.dtype, input.device)
  padded.narrow(0L, 0L, rows).narrow(1L, 0L, cols).copy_(input) |> ignore
  use blocks = padded.view([| rowBlocks; 128L; colBlocks; 4L |]).permute([| 0L; 2L; 1L; 3L |])
  use rearranged = blocks.reshape([| -1L; 4L; 32L; 4L |]).transpose(1L, 2L).reshape([| -1L; 32L; 16L |])
  rearranged.reshape([| -1L |]).contiguous().MoveToOuterDisposeScope()

let mkCfg path backendOverride =
  {
    BackendOverride = backendOverride
    ComputePath = path
    RuntimeTarget = Q4RuntimeTarget.Cpu
    UnifiedMemoryPolicy = UnifiedMemoryPolicy.Disabled
    EnableDiagnostics = true
  }

let tc01 () =
  let tensors =
    [
      "layers.0.weight", torch.zeros([| 16L; 16L |], dtype = torch.float16)
      "layers.0.absmax", torch.ones([| 16L |], dtype = torch.float16)
      "layers.0.quant_map", torch.ones([| 16L |], dtype = torch.float16)
    ]
    |> Map.ofList
  let schema = Schema.detect tensors
  ensure (schema.IsSome && schema.Value.Format = NF4) "TC-01 failed"

let tc02 () =
  let tensors =
    [
      "layers.0.qdata", torch.zeros([| 16L; 16L |], dtype = torch.float16)
      "layers.0.scale", torch.ones([| 16L |], dtype = torch.float16)
    ]
    |> Map.ofList
  let schema = Schema.detect tensors
  ensure (schema.IsSome && schema.Value.Format = NVFP4) "TC-02 failed"

let tc03 () =
  let schema =
    {
      Format = NF4
      WeightKey = "w"
      ScaleKey = None
      AbsmaxKey = Some "missing_absmax"
      QuantMapKey = Some "missing_quant_map"
      ExtraKeys = []
    }
  let tensors = [ "w", torch.zeros([| 7L; 9L |], dtype = torch.float16) ] |> Map.ofList
  match Schema.validate schema tensors with
  | Error errs when errs.Length >= 2 -> ()
  | _ -> failwith "TC-03 failed"

let tc04 () =
  let schema =
    {
      Format = NF4
      WeightKey = "w"
      ScaleKey = None
      AbsmaxKey = Some "a"
      QuantMapKey = Some "q"
      ExtraKeys = []
    }
  let r = Backend.tryCreate schema (mkCfg Q4ComputePath.KernelOrFallback None)
  match r with
  | Ok _ -> ()
  | Error msg -> failwithf "TC-04 failed: %s" msg

let tc05 () =
  NativeInterop.configure(Some "/tmp/notfound_nvfp4.so", Some "/tmp/notfound_nf4.so")
  let schema =
    {
      Format = NF4
      WeightKey = "w"
      ScaleKey = None
      AbsmaxKey = Some "a"
      QuantMapKey = Some "q"
      ExtraKeys = []
    }
  let r = Backend.tryCreate schema (mkCfg Q4ComputePath.KernelOnly None)
  match r with
  | Error _ -> ()
  | Ok backend -> failwithf "TC-05 failed: expected NF4 kernel error but got backend '%s'" backend.Name

let tc06 () =
  let cfg = mkCfg Q4ComputePath.DequantMatmulOnly (Some "dequant-matmul")
  let schema =
    {
      Format = NF4
      WeightKey = "w"
      ScaleKey = None
      AbsmaxKey = Some "a"
      QuantMapKey = Some "q"
      ExtraKeys = []
    }
  let s = Session.create cfg schema
  ensure (s.Backend.Name = "dequant-matmul") "TC-06 failed"

let tc07 () =
  let t = torch.ones([| 4L; 4L |], dtype = torch.float16)
  let t2 = UnifiedMemory.applyPolicy UnifiedMemoryPolicy.PreferUnified t
  ensure (t2.shape = t.shape) "TC-07 failed"

let tc08 () =
  if UnifiedMemory.isSupported() then
    printfn "[TC-08] skipped: environment supports Unified Memory."
  else
    let t = torch.ones([| 2L; 2L |], dtype = torch.float16)
    ensure (expectThrows (fun () -> ignore (UnifiedMemory.applyPolicy UnifiedMemoryPolicy.RequireUnified t))) "TC-08 failed"

let tc09 () =
  let cfg = mkCfg Q4ComputePath.DequantMatmulOnly (Some "dequant-matmul")
  let schema =
    {
      Format = NF4
      WeightKey = "w"
      ScaleKey = None
      AbsmaxKey = Some "a"
      QuantMapKey = Some "q"
      ExtraKeys = []
    }
  let bundle =
    {
      Weight = torch.randn([| 3L; 4L |], dtype = torch.float16)
      Scale = None
      Absmax = None
      QuantMap = None
    }
  let x = torch.randn([| 2L; 4L |], dtype = torch.float16)
  let s = Session.create cfg schema
  use lin = s.CreateLinear(bundle)
  let y = lin.Forward(x)
  ensure (y.shape = [| 2L; 3L |]) "TC-09 failed"

let tc10 () =
  let cfg = mkCfg Q4ComputePath.DequantMatmulOnly (Some "dequant-matmul")
  let schema =
    {
      Format = NF4
      WeightKey = "w"
      ScaleKey = None
      AbsmaxKey = Some "a"
      QuantMapKey = Some "q"
      ExtraKeys = []
    }
  let bundle =
    {
      Weight = torch.randn([| 3L; 4L |], dtype = torch.float16)
      Scale = None
      Absmax = None
      QuantMap = None
    }
  let x = torch.randn([| 2L; 5L |], dtype = torch.float16)
  let s = Session.create cfg schema
  use lin = s.CreateLinear(bundle)
  ensure (expectThrows (fun () -> ignore (lin.Forward(x)))) "TC-10 failed"

let tc11 () =
  let schema =
    {
      Format = NF4
      WeightKey = "w"
      ScaleKey = None
      AbsmaxKey = Some "a"
      QuantMapKey = Some "q"
      ExtraKeys = []
    }
  let tensors =
    [
      "w", torch.zeros([| 7L; 9L |], dtype = torch.float16)
      "a", torch.ones([| 1L |], dtype = torch.float16)
      "q", torch.ones([| 16L |], dtype = torch.float16)
    ]
    |> Map.ofList
  match Schema.validate schema tensors with
  | Error errs ->
    ensure (errs |> List.exists (fun e -> e.Contains("aligned to 8"))) "TC-11 failed"
  | _ -> failwith "TC-11 failed"

let tc12 () =
  let schema =
    {
      Format = NVFP4
      WeightKey = "q"
      ScaleKey = Some "s"
      AbsmaxKey = None
      QuantMapKey = None
      ExtraKeys = []
    }
  let r = Backend.tryCreate schema (mkCfg Q4ComputePath.KernelOnly (Some "unknown"))
  match r with
  | Error msg ->
    ensure (msg.Contains("Unknown backend override") || msg.Contains("KernelOnly")) "TC-12 failed"
  | _ -> failwith "TC-12 failed"

let tc13 () =
  let t = torch.randn([| 2L; 2L |], dtype = torch.float16)
  let t2 = UnifiedMemory.applyMutablePolicy UnifiedMemoryPolicy.PreferUnified t
  ensure (not (Object.ReferenceEquals(t, t2))) "TC-13 failed"

let tc14 () =
  let cuda = torch.cuda_is_available()
  let hasQuant = NativeInterop.hasLibTorchFp4Quantize()
  let hasScaled = NativeInterop.hasLibTorchScaledMm()
  if not (cuda && hasQuant && hasScaled) then
    printfn "[TC-14] probe: cuda=%b quant=%b scaled=%b" cuda hasQuant hasScaled
    printfn "[TC-14] skipped: CUDA or LibTorch FP4 exports unavailable."
  else
    use x = torch.randn([| 8L; 64L |], dtype = torch.float16, device = torch.CUDA)
    let q, s = NativeInterop.fp4Quantize x
    use qd = q
    use sd = s
    ensure (qd.shape = [| 8L; 32L |]) "TC-14 failed: qdata shape mismatch"
    ensure (sd.shape = [| 8L; 4L |]) "TC-14 failed: scale shape mismatch"

let tc15 () =
  let cuda = torch.cuda_is_available()
  let hasQuant = NativeInterop.hasLibTorchFp4Quantize()
  let hasScaled = NativeInterop.hasLibTorchScaledMm()
  if not (cuda && hasQuant && hasScaled) then
    printfn "[TC-15] probe: cuda=%b quant=%b scaled=%b" cuda hasQuant hasScaled
    printfn "[TC-15] skipped: CUDA or LibTorch FP4 exports unavailable."
  else
    use x = torch.randn([| 4L; 64L |], dtype = torch.float16, device = torch.CUDA)
    use w = torch.randn([| 16L; 64L |], dtype = torch.float16, device = torch.CUDA)

    let qxRaw, sxRaw = NativeInterop.fp4Quantize x
    let qwRaw, swRaw = NativeInterop.fp4Quantize w
    use qx = qxRaw
    use sx = sxRaw
    use qw = qwRaw
    use sw = swRaw

    use sxBlocked = toBlocked sx
    use swBlocked = toBlocked sw
    use y = NativeInterop.scaledMmFp4 qx (qw.t()) sxBlocked swBlocked torch.float16

    ensure (y.shape = [| 4L; 16L |]) "TC-15 failed: output shape mismatch"

    use anyNan = torch.isnan(y).any()
    let hasNan = anyNan.cpu().item<bool>()
    ensure (not hasNan) "TC-15 failed: output contains NaN"

    use yF = y.to_type(torch.float32)
    use xF = x.to_type(torch.float32)
    use wF = w.to_type(torch.float32)
    use refOut = torch.matmul(xF, wF.t())
    use absErr = (yF - refOut).abs()
    use errMean = absErr.mean()
    use refMean = refOut.abs().mean() + 1e-6
    use rel = errMean / refMean
    let relValue = rel.cpu().item<float32>()
    ensure (Single.IsFinite(relValue)) "TC-15 failed: relative error is not finite"
    ensure (relValue < 1.5f) (sprintf "TC-15 failed: relative error too large (%f)" relValue)

let tc16 () =
  let cuda = torch.cuda_is_available()
  let hasQuant = NativeInterop.hasLibTorchFp4Quantize()
  let hasScaled = NativeInterop.hasLibTorchScaledMm()
  if not (cuda && hasQuant && hasScaled) then
    printfn "[TC-16] skipped: CUDA or LibTorch FP4 exports unavailable."
  else
    let cfg =
      {
        BackendOverride = None
        ComputePath = Q4ComputePath.KernelOnly
        RuntimeTarget = Q4RuntimeTarget.Cuda 0
        UnifiedMemoryPolicy = UnifiedMemoryPolicy.Disabled
        EnableDiagnostics = true
      }
    let schema =
      {
        Format = NVFP4
        WeightKey = "q"
        ScaleKey = Some "s"
        AbsmaxKey = None
        QuantMapKey = None
        ExtraKeys = []
      }
    match Backend.tryCreate schema cfg with
    | Ok backend ->
      ensure (backend.Name = "nvfp4-kernel") (sprintf "TC-16 failed: unexpected backend '%s'" backend.Name)
    | Error err ->
      failwithf "TC-16 failed: %s" err

let tc17 () =
  let schema =
    {
      Format = NVFP4
      WeightKey = "q"
      ScaleKey = Some "s"
      AbsmaxKey = None
      QuantMapKey = None
      ExtraKeys = []
    }

  let cfg =
    {
      BackendOverride = None
      ComputePath = Q4ComputePath.KernelOnly
      RuntimeTarget = Q4RuntimeTarget.Cpu
      UnifiedMemoryPolicy = UnifiedMemoryPolicy.Disabled
      EnableDiagnostics = true
    }

  match Backend.tryCreate schema cfg with
  | Ok backend ->
    failwithf "TC-17 failed: expected failure for CPU kernel-only path, got '%s'" backend.Name
  | Error _ -> ()

let tc18 () =
  use w = torch.randn([| 16L; 64L |], dtype = torch.float32, device = "cpu")
  let q, s = Nvfp4Training.quantizePacked w
  use qd = q
  use sd = s
  ensure (qd.dtype = torch.uint8) "TC-18 failed: qdata dtype mismatch"
  ensure (qd.shape = [| 16L; 32L |]) "TC-18 failed: qdata shape mismatch"
  ensure (sd.shape = [| 16L; 4L |]) "TC-18 failed: scale shape mismatch"
  use dq = Nvfp4Training.dequantizePacked qd sd torch.float32
  ensure (dq.shape = w.shape) "TC-18 failed: dequantized shape mismatch"
  use rel = (dq - w).abs().mean() / (w.abs().mean() + 1e-6)
  let relValue = rel.item<float32>()
  ensure (Single.IsFinite(relValue)) "TC-18 failed: relative error is not finite"

let tc19 () =
  use x = torch.randn([| 4L; 64L |], dtype = torch.float32, device = "cpu")
  use w0 = torch.randn([| 16L; 64L |], dtype = torch.float32, device = "cpu")
  use w = torch.nn.Parameter(w0.clone(), true)
  use y = Nvfp4Training.linearSte x w torch.float32
  use loss = (y * y).mean()
  loss.backward()
  ensure (not (isNull w.grad)) "TC-19 failed: grad is null"
  use gradNorm = w.grad.abs().sum()
  let gradNormValue = gradNorm.item<float32>()
  ensure (Single.IsFinite(gradNormValue) && gradNormValue > 0.0f) "TC-19 failed: grad norm invalid"

let tc20 () =
  use x = torch.randn([| 2L; 64L |], dtype = torch.float32, device = "cpu")
  use w0 = torch.randn([| 16L; 64L |], dtype = torch.float32, device = "cpu")
  use w = torch.nn.Parameter(w0.clone(), true)

  for _ in 1 .. 128 do
    use y = Nvfp4Training.linearSte x w torch.float32
    use loss = (y * y).mean()
    loss.backward()
    ensure (not (isNull w.grad)) "TC-20 failed: grad is null"
    use gradNorm = w.grad.abs().sum()
    let gradNormValue = gradNorm.item<float32>()
    ensure (Single.IsFinite(gradNormValue)) "TC-20 failed: grad norm not finite"
    w.grad.zero_() |> ignore

let cases =
  [
    "TC-01", tc01
    "TC-02", tc02
    "TC-03", tc03
    "TC-04", tc04
    "TC-05", tc05
    "TC-06", tc06
    "TC-07", tc07
    "TC-08", tc08
    "TC-09", tc09
    "TC-10", tc10
    "TC-11", tc11
    "TC-12", tc12
    "TC-13", tc13
    "TC-14", tc14
    "TC-15", tc15
    "TC-16", tc16
    "TC-17", tc17
    "TC-18", tc18
    "TC-19", tc19
    "TC-20", tc20
  ]

printfn "[TC] running %d tests" cases.Length
for (name, run) in cases do
  run ()
  printfn "[PASS] %s" name
