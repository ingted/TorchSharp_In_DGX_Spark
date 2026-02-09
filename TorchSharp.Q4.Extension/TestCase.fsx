#if INTERACTIVE
#r "nuget: FAkka.TorchSharp.DGX, 26.1.0-py3.5"
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
      Format = NVFP4
      WeightKey = "q"
      ScaleKey = Some "s"
      AbsmaxKey = None
      QuantMapKey = None
      ExtraKeys = []
    }
  let r = Backend.tryCreate schema (mkCfg Q4ComputePath.KernelOnly None)
  match r with
  | Error _ -> ()
  | Ok backend -> failwithf "TC-05 failed: expected error but got backend '%s'" backend.Name

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
  ]

printfn "[TC] running %d tests" cases.Length
for (name, run) in cases do
  run ()
  printfn "[PASS] %s" name
