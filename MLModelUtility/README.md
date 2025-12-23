# MLModelUtility

AIモデルの入出力を抽象化し、複数のフォーマットに対応するためのライブラリです。

## 概要

このライブラリは、機械学習モデルのテンソルデータと計算グラフを読み書きするための統一的なインターフェースを提供します。Safetensors、ONNX、Unity Sentis フォーマットをサポートしています。

## 対応フォーマット

| フォーマット | テンソル読込 | テンソル書込 | グラフ読込 | グラフ書込 | 状態 |
|------------|:----------:|:----------:|:--------:|:--------:|:----:|
| Safetensors | ✅ | ✅ | - | - | 完全実装 |
| ONNX | ✅ | - | ✅ | ✅ | 完全実装 |
| Sentis | - | ✅ | - | ✅ | 完全実装 |

> **Note**: ONNX フォーマットではテンソルのみの書き込みはサポートされません。グラフと一緒に `WriteGraphWithTensors` を使用してください。

> **Note**: Sentis フォーマットは Unity の推論エンジン用であり、出力（書き込み）のみをサポートしています。読み込みはサポートされません。

## インストール

プロジェクトに `MLModelUtility` を参照として追加してください。

## 基本的な使い方

### Safetensors ファイルの読み込み

```csharp
using MLModelUtility.Formats.Safetensors;
using MLModelUtility.Models;

// ハンドラのインスタンスを作成
var handler = new SafetensorsFormatHandler();

// 同期読み込み
using TensorCollection tensors = handler.ReadTensorsFromFile("model.safetensors");

// 各テンソルにアクセス
foreach (var tensor in tensors)
{
    Console.WriteLine($"Name: {tensor.Info.Name}");
    Console.WriteLine($"Shape: {tensor.Info.ShapeToString()}");
    Console.WriteLine($"DataType: {tensor.Info.DataType}");
    Console.WriteLine($"ByteSize: {tensor.Info.ByteSize}");
}

// 名前でテンソルを取得
if (tensors.TryGetTensor("weight", out var weightTensor))
{
    // Float32データとして取得
    ReadOnlySpan<float> data = weightTensor!.GetDataAs<float>();
    Console.WriteLine($"First value: {data[0]}");
}
```

### 非同期読み込み

```csharp
using MLModelUtility.Formats.Safetensors;

var handler = new SafetensorsFormatHandler();

// 非同期読み込み（キャンセルトークン対応）
using var cts = new CancellationTokenSource();
using TensorCollection tensors = await handler.ReadTensorsFromFileAsync(
    "model.safetensors", 
    cts.Token
);
```

### Safetensors ファイルの書き込み

```csharp
using MLModelUtility.Formats.Safetensors;
using MLModelUtility.Models;

// テンソルデータを作成
var weight = TensorData.FromFloat32(
    name: "layer.weight",
    shape: [128, 64],
    values: new float[128 * 64]  // 実際のデータ
);

var bias = TensorData.FromFloat32(
    name: "layer.bias",
    shape: [128],
    values: new float[128]
);

// メタデータ（オプション）
var metadata = new Dictionary<string, string>
{
    ["format"] = "pt",
    ["framework"] = "custom"
};

// コレクションを作成
using var tensors = new TensorCollection([weight, bias], metadata);

// ファイルに書き込み
var handler = new SafetensorsFormatHandler();
handler.WriteTensorsToFile(tensors, "output.safetensors");

// 非同期書き込み
await handler.WriteTensorsToFileAsync(tensors, "output.safetensors");
```

### ストリームを使用した読み書き

```csharp
using MLModelUtility.Formats.Safetensors;

var handler = new SafetensorsFormatHandler();

// ストリームから読み込み
using var inputStream = File.OpenRead("model.safetensors");
using var tensors = handler.ReadTensors(inputStream);

// ストリームに書き込み
using var outputStream = File.Create("output.safetensors");
handler.WriteTensors(tensors, outputStream);
```

### ONNX ファイルの読み込み

```csharp
using MLModelUtility.Formats.Onnx;
using MLModelUtility.Models;
using MLModelUtility.Models.Graph;

var handler = new OnnxFormatHandler();

// グラフとテンソルを同時に読み込む
var (graph, tensors) = handler.ReadGraphWithTensors(File.OpenRead("model.onnx"));

Console.WriteLine($"Graph: {graph.Name}");
Console.WriteLine($"IR Version: {graph.IrVersion}");
Console.WriteLine($"Nodes: {graph.Nodes.Count}");
Console.WriteLine($"Tensors: {tensors.Count}");

// ノードを列挙
foreach (var node in graph.Nodes)
{
    Console.WriteLine($"  {node.OperatorType}: {node.Name}");
}

// 入出力情報
foreach (var input in graph.Inputs)
{
    Console.WriteLine($"Input: {input.Name} {input.ShapeToString()}");
}

// テンソルデータにアクセス
foreach (var tensor in tensors)
{
    Console.WriteLine($"Weight: {tensor.Info.Name} - {tensor.Info.DataType} {tensor.Info.ShapeToString()}");
}

tensors.Dispose();
```

### ONNX ファイルの書き込み

```csharp
using MLModelUtility.Formats.Onnx;
using MLModelUtility.Models;
using MLModelUtility.Models.Graph;

// 計算グラフを作成
var graph = new ComputeGraph
{
    Name = "simple_model",
    IrVersion = 8,
    ProducerName = "MLModelUtility",
    ProducerVersion = "1.0.0"
};

// Opsetバージョンを設定
graph.OpsetVersions[""] = 17;

// 入力を定義
graph.Inputs.Add(new TensorInfo("input", [1, 3, 224, 224], TensorDataType.Float32));

// 出力を定義
graph.Outputs.Add(new TensorInfo("output", [1, 1000], TensorDataType.Float32));

// ノードを追加
var node = new GraphNode
{
    Name = "conv1",
    OperatorType = "Conv",
    Inputs = ["input", "conv1.weight", "conv1.bias"],
    Outputs = ["conv1_output"]
};
node.Attributes["kernel_shape"] = new long[] { 3, 3 };
node.Attributes["strides"] = new long[] { 1, 1 };
graph.Nodes.Add(node);

// テンソルデータ（重み）を作成
var weight = TensorData.FromFloat32("conv1.weight", [64, 3, 3, 3], new float[64 * 3 * 3 * 3]);
var bias = TensorData.FromFloat32("conv1.bias", [64], new float[64]);
using var tensors = new TensorCollection([weight, bias]);

// ファイルに書き込み
var handler = new OnnxFormatHandler();
handler.WriteGraphWithTensors(graph, tensors, File.Create("output.onnx"));
```

### Unity Sentis ファイルの書き込み

```csharp
using MLModelUtility.Formats.Sentis;
using MLModelUtility.Models;
using MLModelUtility.Models.Graph;

// 計算グラフを作成
var graph = new ComputeGraph
{
    Name = "unity_model",
    IrVersion = 1,
    ProducerName = "MLModelUtility",
    ProducerVersion = "1.0.0"
};

// 入力を定義
graph.Inputs.Add(new TensorInfo("input", [1, 3, 224, 224], TensorDataType.Float32));

// 出力を定義
graph.Outputs.Add(new TensorInfo("output", [1, 1000], TensorDataType.Float32));

// ノードを追加
var node = new GraphNode
{
    Name = "dense1",
    OperatorType = "MatMul",
    Inputs = ["input", "dense1.weight"],
    Outputs = ["dense1_output"]
};
graph.Nodes.Add(node);

// テンソルデータ（重み）を作成
var weight = TensorData.FromFloat32("dense1.weight", [1000, 3 * 224 * 224], new float[1000 * 3 * 224 * 224]);
using var tensors = new TensorCollection([weight]);

// Sentis形式でファイルに書き込み
var handler = new SentisFormatHandler();
handler.WriteGraphWithTensors(graph, tensors, File.Create("model.sentis"));

// 非同期書き込み
await handler.WriteGraphWithTensorsAsync(graph, tensors, File.Create("model.sentis"));
```

> **Note**: Sentis フォーマットは Unity の Inference Engine (Sentis) で直接ロードできる形式です。
> 対応するデータ型は Float32, Int32, UInt8 (Byte), Int16 (Short) に限定されます。

## テンソルデータの作成

### 各データ型のファクトリメソッド

```csharp
using MLModelUtility.Models;

// Float32
var float32Tensor = TensorData.FromFloat32("tensor_f32", [2, 3], new float[6]);

// Float64
var float64Tensor = TensorData.FromFloat64("tensor_f64", [2, 3], new double[6]);

// Int32
var int32Tensor = TensorData.FromInt32("tensor_i32", [2, 3], new int[6]);

// Int64
var int64Tensor = TensorData.FromInt64("tensor_i64", [2, 3], new long[6]);

// 空のテンソル（ゼロ初期化）
var emptyTensor = TensorData.CreateEmpty("empty", [10, 10], TensorDataType.Float32);
```

### バイトデータから直接作成

```csharp
using MLModelUtility.Models;

var info = new TensorInfo("custom", [4, 4], TensorDataType.Float16);
byte[] rawData = new byte[info.ByteSize];
// rawData にデータを設定...

var tensor = new TensorData(info, rawData);
```

## フォーマット対応能力の確認

各フォーマットハンドラは `ModelFormatCapability` フラグで対応機能を公開しています。

```csharp
using MLModelUtility.Capabilities;
using MLModelUtility.Formats.Safetensors;
using MLModelUtility.Formats.Onnx;

var safetensors = new SafetensorsFormatHandler();
var onnx = new OnnxFormatHandler();

// Capability プロパティで確認
Console.WriteLine($"Safetensors: {safetensors.Capability}");  // TensorOnly
Console.WriteLine($"ONNX: {onnx.Capability}");  // TensorRead | GraphRead | GraphWrite

// 特定機能のサポート確認
if (safetensors.Supports(ModelFormatCapability.TensorWrite))
{
    Console.WriteLine("Safetensors はテンソル書き込みをサポート");
}

if (!safetensors.Supports(ModelFormatCapability.GraphRead))
{
    Console.WriteLine("Safetensors はグラフ読み込みをサポートしない");
}
```

### Capability フラグ一覧

| フラグ | 説明 |
|-------|------|
| `None` | 機能なし |
| `TensorRead` | テンソルデータの読み込み |
| `TensorWrite` | テンソルデータの書き込み |
| `GraphRead` | 計算グラフの読み込み |
| `GraphWrite` | 計算グラフの書き込み |
| `TensorOnly` | `TensorRead \| TensorWrite` |
| `GraphOnly` | `GraphRead \| GraphWrite` |
| `Full` | すべての機能 |

## 対応データ型

| TensorDataType | Safetensors表記 | バイトサイズ |
|----------------|----------------|-------------|
| `Float32` | F32 | 4 |
| `Float64` | F64 | 8 |
| `Float16` | F16 | 2 |
| `BFloat16` | BF16 | 2 |
| `UInt8` | U8 | 1 |
| `Int8` | I8 | 1 |
| `UInt16` | U16 | 2 |
| `Int16` | I16 | 2 |
| `UInt32` | U32 | 4 |
| `Int32` | I32 | 4 |
| `UInt64` | U64 | 8 |
| `Int64` | I64 | 8 |
| `Bool` | BOOL | 1 |

## アーキテクチャ

### ディレクトリ構造

```
MLModelUtility/
├── Capabilities/
│   └── ModelFormatCapability.cs    # フォーマット機能フラグ
├── IO/
│   ├── IModelFormatHandler.cs      # フォーマットハンドラ基底インターフェース
│   ├── ITensorReader.cs            # テンソル読み込みインターフェース
│   ├── ITensorWriter.cs            # テンソル書き込みインターフェース
│   ├── IGraphReader.cs             # 計算グラフ読み込みインターフェース
│   └── IGraphWriter.cs             # 計算グラフ書き込みインターフェース
├── Models/
│   ├── TensorDataType.cs           # データ型列挙
│   ├── TensorInfo.cs               # テンソルメタデータ
│   ├── ITensorData.cs              # テンソルデータインターフェース
│   ├── TensorData.cs               # オンメモリ実装
│   ├── TensorCollection.cs         # テンソルコレクション
│   └── Graph/
│       ├── GraphNode.cs            # 計算グラフノード
│       └── ComputeGraph.cs         # 計算グラフコンテナ
└── Formats/
    ├── Safetensors/
    │   ├── SafetensorsHeader.cs
    │   └── SafetensorsFormatHandler.cs
    ├── Onnx/
    │   ├── Generated/              # protobuf生成コード
    │   └── OnnxFormatHandler.cs
    └── Sentis/
        ├── Generated/              # FlatBuffers生成コード
        └── SentisFormatHandler.cs
```

### クラス図

```
IModelFormatHandler
    ├── FormatName: string
    ├── FileExtension: string
    ├── Capability: ModelFormatCapability
    └── Supports(capability): bool

ITensorReader
    ├── ReadTensors(stream): TensorCollection
    ├── ReadTensorsAsync(stream, ct): Task<TensorCollection>
    ├── ReadTensorsFromFile(path): TensorCollection
    └── ReadTensorsFromFileAsync(path, ct): Task<TensorCollection>

ITensorWriter
    ├── WriteTensors(tensors, stream): void
    ├── WriteTensorsAsync(tensors, stream, ct): Task
    ├── WriteTensorsToFile(tensors, path): void
    └── WriteTensorsToFileAsync(tensors, path, ct): Task

ITensorData (implements IDisposable)
    ├── Info: TensorInfo
    ├── GetDataSpan(): ReadOnlySpan<byte>
    ├── GetDataMemory(): ReadOnlyMemory<byte>
    └── GetDataAs<T>(): ReadOnlySpan<T>

TensorCollection (implements IReadOnlyList<ITensorData>, IDisposable)
    ├── Metadata: IReadOnlyDictionary<string, string>?
    ├── this[int index]: ITensorData
    ├── this[string name]: ITensorData
    ├── ContainsTensor(name): bool
    ├── TryGetTensor(name, out tensor): bool
    └── GetTensorNames(): IEnumerable<string>
```

## 拡張ポイント

### メモリマップ対応（将来）

`ITensorData` インターフェースにより、オンメモリ以外のバックエンドも実装可能です。

```csharp
// 将来的な MemoryMappedTensorData の使用例
public class MemoryMappedTensorData : ITensorData
{
    // メモリマップファイルを使用した大規模テンソルの効率的な読み込み
}
```

### 新しいフォーマットの追加

1. `IModelFormatHandler` を実装
2. 必要に応じて `ITensorReader`, `ITensorWriter`, `IGraphReader`, `IGraphWriter` を実装
3. `Capability` プロパティで対応機能を宣言

## ライセンス

zlib License

Copyright (c) 2025 Sinoa

