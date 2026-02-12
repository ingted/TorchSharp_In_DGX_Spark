namespace TorchSharp.Q4.Extension

open System
open TorchSharp

module SchemaImpl =
  let hasSuffix (suffix: string) (key: string) =
    key.EndsWith(suffix, StringComparison.Ordinal)

  let prefixOf (suffix: string) (key: string) =
    key.Substring(0, key.Length - suffix.Length)

  let prefixesBySuffix (suffix: string) (keys: string list) =
    keys
    |> List.choose (fun key ->
      if hasSuffix suffix key then
        Some (prefixOf suffix key)
      else
        None)
    |> Set.ofList

  let knownKeysFor (prefix: string) (format: QuantFormat) =
    match format with
    | NF4 -> Set.ofList [ prefix + ".weight"; prefix + ".absmax"; prefix + ".quant_map" ]
    | NVFP4 -> Set.ofList [ prefix + ".qdata"; prefix + ".scale" ]

  let buildSchema (keys: string list) (prefix: string) (format: QuantFormat) =
    let known = knownKeysFor prefix format
    let weightKey, scaleKey, absmaxKey, quantMapKey =
      match format with
      | NF4 -> prefix + ".weight", None, Some (prefix + ".absmax"), Some (prefix + ".quant_map")
      | NVFP4 -> prefix + ".qdata", Some (prefix + ".scale"), None, None

    let extraKeys =
      keys
      |> List.filter (fun key ->
        key.StartsWith(prefix + ".", StringComparison.Ordinal) && not (known.Contains(key)))
      |> List.sort

    {
      Format = format
      WeightKey = weightKey
      ScaleKey = scaleKey
      AbsmaxKey = absmaxKey
      QuantMapKey = quantMapKey
      ExtraKeys = extraKeys
    }

  let tensorElementsByShape (tensor: TorchSharp.torch.Tensor) =
    tensor.shape |> Array.fold (fun acc dim -> acc * dim) 1L

  let validateMatrixLike (key: string) (tensor: TorchSharp.torch.Tensor) (errors: ResizeArray<string>) =
    let shape = tensor.shape
    if shape.Length < 2 then
      errors.Add(sprintf "%s: expected rank >= 2, got rank=%d" key shape.Length)
    else
      let rowDim = shape.[shape.Length - 2]
      let colDim = shape.[shape.Length - 1]
      if rowDim <= 0L || colDim <= 0L then
        errors.Add(sprintf "%s: invalid matrix dims (%d, %d)" key rowDim colDim)
      if rowDim % 8L <> 0L || colDim % 8L <> 0L then
        errors.Add(sprintf "%s: matrix dims (%d, %d) are not aligned to 8" key rowDim colDim)

  let tryGetTensor (key: string) (tensors: Map<string, TorchSharp.torch.Tensor>) (errors: ResizeArray<string>) =
    match tensors.TryFind key with
    | Some tensor -> Some tensor
    | None ->
      errors.Add(sprintf "Missing required tensor key: %s" key)
      None

  let validateOptionalTensorKey
    (label: string)
    (key: string option)
    (tensors: Map<string, TorchSharp.torch.Tensor>)
    (errors: ResizeArray<string>)
    =
    match key with
    | Some k -> tryGetTensor k tensors errors
    | None ->
      errors.Add(sprintf "Schema missing required %s key." label)
      None

module Schema =
  let rec detect (tensors: Map<string, TorchSharp.torch.Tensor>) : Q4Schema option =
    let keys = tensors |> Map.keys |> Seq.toList |> List.sort

    let nf4Prefixes =
      Set.intersect
        (SchemaImpl.prefixesBySuffix ".absmax" keys)
        (SchemaImpl.prefixesBySuffix ".quant_map" keys)
      |> Set.filter (fun prefix -> tensors.ContainsKey(prefix + ".weight"))

    let nvfp4Prefixes =
      Set.intersect
        (SchemaImpl.prefixesBySuffix ".qdata" keys)
        (SchemaImpl.prefixesBySuffix ".scale" keys)

    match nvfp4Prefixes |> Set.toList |> List.sort, nf4Prefixes |> Set.toList |> List.sort with
    | nvPrefix :: _, _ -> Some (SchemaImpl.buildSchema keys nvPrefix NVFP4)
    | [], nfPrefix :: _ -> Some (SchemaImpl.buildSchema keys nfPrefix NF4)
    | [], [] -> None

  and detectOrFail (tensors: Map<string, TorchSharp.torch.Tensor>) : Q4Schema =
    match detect tensors with
    | None ->
      raise
        (InvalidOperationException(
          "Schema detection failed: no valid NF4(.weight/.absmax/.quant_map) or NVFP4(.qdata/.scale) group found."
        ))
    | Some schema ->
      match validate schema tensors with
      | Ok () -> schema
      | Error errors ->
        let message = "Schema detected but validation failed:\n" + String.concat "\n" errors
        raise (InvalidOperationException(message))

  and validate (schema: Q4Schema) (tensors: Map<string, TorchSharp.torch.Tensor>) : Result<unit, string list> =
    let errors = ResizeArray<string>()

    let weightOpt = SchemaImpl.tryGetTensor schema.WeightKey tensors errors
    match weightOpt with
    | Some weight ->
      SchemaImpl.validateMatrixLike schema.WeightKey weight errors
      if SchemaImpl.tensorElementsByShape weight <= 0L then
        errors.Add(sprintf "%s: tensor has zero elements." schema.WeightKey)
    | None -> ()

    match schema.Format with
    | NF4 ->
      let absmaxOpt = SchemaImpl.validateOptionalTensorKey "absmax" schema.AbsmaxKey tensors errors
      let quantMapOpt = SchemaImpl.validateOptionalTensorKey "quant_map" schema.QuantMapKey tensors errors

      match absmaxOpt, quantMapOpt, weightOpt with
      | Some absmax, Some quantMap, Some weight ->
        let absmaxCount = SchemaImpl.tensorElementsByShape absmax
        let quantMapCount = SchemaImpl.tensorElementsByShape quantMap
        let weightCount = SchemaImpl.tensorElementsByShape weight
        if absmaxCount <= 0L then
          errors.Add(sprintf "%s: tensor has zero elements." schema.AbsmaxKey.Value)
        if quantMapCount <= 0L then
          errors.Add(sprintf "%s: tensor has zero elements." schema.QuantMapKey.Value)
        if absmaxCount > 0L && weightCount % absmaxCount <> 0L then
          errors.Add(
            sprintf
              "%s: weight element count (%d) is not divisible by absmax element count (%d)."
              schema.AbsmaxKey.Value
              weightCount
              absmaxCount
          )
      | _ -> ()

    | NVFP4 ->
      let scaleOpt = SchemaImpl.validateOptionalTensorKey "scale" schema.ScaleKey tensors errors

      match scaleOpt, weightOpt with
      | Some scale, Some weight ->
        let scaleCount = SchemaImpl.tensorElementsByShape scale
        let weightCount = SchemaImpl.tensorElementsByShape weight
        if scaleCount <= 0L then
          errors.Add(sprintf "%s: tensor has zero elements." schema.ScaleKey.Value)
        if scaleCount > weightCount then
          errors.Add(
            sprintf
              "%s: scale element count (%d) cannot exceed weight element count (%d)."
              schema.ScaleKey.Value
              scaleCount
              weightCount
          )
      | _ -> ()

    if errors.Count = 0 then Ok () else Error (errors |> Seq.toList)
