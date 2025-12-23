# MLModelUtility

AIモデルの入出力を抽象化し、複数のフォーマットに対応するためのライブラリです。

## 概要

このライブラリは、機械学習モデルのテンソルデータと計算グラフを読み書きするための統一的なインターフェースを提供します。現在は Safetensors フォーマットをサポートしており、将来的に ONNX フォーマットにも対応予定です。

## 対応フォーマット

| フォーマット | テンソル読込 | テンソル書込 | グラフ読込 | グラフ書込 | 状態 |
|------------|:----------:|:----------:|:--------:|:--------:|:----:|
| Safetensors | ✅ | ✅ | - | - | 完全実装 |
| ONNX | 🚧 | - | 🚧 | 🚧 | 未実装 |

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
    └── Onnx/
        ├── Generated/              # protobuf生成コード（将来）
        └── OnnxFormatHandler.cs
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

