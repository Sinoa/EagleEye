# MLModelCodec

TorchSharpで構築した機械学習モデルをONNX形式にエンコードするための .NET ライブラリです。

## 動作要件

- .NET 10.0 以降
- TorchSharp-cpu 0.105.2
- Google.Protobuf（ONNX Protocol Buffers）

## 名前空間

```
Foxtamp.MLModelCodec.Encoders
Foxtamp.MLModelCodec.Models.Abstract
Foxtamp.MLModelCodec.Utilities
```

## 機能概要

### エンコーダー

| クラス | 説明 |
|--------|------|
| `MLModelEncoder` | `MLAbstractModel`をONNX形式（`ModelProto`）にエンコードするクラス |

### 抽象モデル

| クラス | 説明 |
|--------|------|
| `MLAbstractModel` | ONNXモデルの抽象表現。メタデータ、グラフ、作者情報などを保持 |
| `MLAbstractGraph` | 計算グラフの抽象表現。入出力、ノード、初期化子を保持 |
| `MLAbstractNode` | 演算ノードの抽象表現。演算子タイプ、入出力名、属性を保持 |
| `MLAbstractTensor` | テンソルの抽象表現。形状、データ型、値を保持 |
| `MLAbstractValueInfo` | 入出力テンソルの型情報の抽象表現 |
| `MLAbstractAttribute` | ノード属性の抽象表現 |

### ユーティリティ

| クラス | 説明 |
|--------|------|
| `OnnxUtility` | ONNX Protocol Bufferオブジェクト（`ModelProto`、`GraphProto`、`NodeProto`等）を生成するユーティリティ |

---

## APIリファレンス

### MLModelEncoder クラス

`MLAbstractModel`をONNX形式にエンコードするメインクラスです。

#### メソッド

##### Encode

```csharp
public ModelProto Encode(MLAbstractModel model)
```

`MLAbstractModel`を`ModelProto`にエンコードします。

| パラメータ | 型 | 説明 |
|-----------|-----|------|
| `model` | `MLAbstractModel` | エンコードするモデル |

**戻り値**: 変換された`ModelProto`インスタンス

##### Export（ストリーム）

```csharp
public void Export(MLAbstractModel model, Stream stream)
```

`MLAbstractModel`をONNXバイナリ形式でストリームに出力します。

##### Export（ファイル）

```csharp
public void Export(MLAbstractModel model, string path)
```

`MLAbstractModel`をONNXバイナリ形式でファイルに出力します。

---

### OnnxUtility クラス

ONNX Protocol Bufferオブジェクトを生成するユーティリティクラスです。

#### 主要メソッド

| メソッド | 説明 |
|---------|------|
| `CreateModel(...)` | `ModelProto`を作成 |
| `CreateGraph(...)` | `GraphProto`を作成 |
| `CreateNode(...)` | `NodeProto`を作成 |
| `CreateValueInfo(...)` | `ValueInfoProto`を作成 |
| `CreateTensorProto(...)` | `TensorProto`を作成 |
| `CreateAttribute(...)` | `AttributeProto`を作成 |

---

## 使用例

### 基本的な使い方

```csharp
using Foxtamp.MLModelCodec.Encoders;
using Foxtamp.MLModelCodec.Models.Abstract;

// 抽象モデルを構築
var model = new MLAbstractModel
{
    ProducerName = "MyApplication",
    ProducerVersion = "1.0.0",
    Author = "Developer",
    Domain = "ai.example",
    Version = 1,
    Graph = graph,  // MLAbstractGraph
};

// ONNXにエンコードしてファイルに出力
var encoder = new MLModelEncoder();
encoder.Export(model, "model.onnx");
```

### ModelProtoとして取得

```csharp
var encoder = new MLModelEncoder();
var modelProto = encoder.Encode(model);

// 追加のカスタマイズ
modelProto.DocString = "Custom documentation";
```

---

## 関連ドキュメント

- [MLCoreModule README](../MLCoreModule/README.md) - アテンション機構・位置エンコーディングライブラリ

## ライセンス

zlib License - Copyright (c) 2026 Sinoa
