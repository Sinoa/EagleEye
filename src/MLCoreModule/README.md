# MLCoreModule

TorchSharpをベースとした機械学習コアモジュールライブラリです。Transformerアーキテクチャで使用されるアテンション機構と位置エンコーディングの実装を提供します。

## 動作要件

- .NET 10.0 以降
- TorchSharp-cpu 0.105.2

## 名前空間

```
Foxtamp.MLCoreModule.Modules
Foxtamp.MLCoreModule.PositionalEncodings
Foxtamp.MLCoreModule.Utilities
```

## 機能概要

### アテンション機構

| クラス | 説明 |
|--------|------|
| `SelfAttention` | シングルヘッド・セルフアテンション。入力シーケンス自身に対してアテンションを計算します。 |
| `CrossAttention` | シングルヘッド・クロスアテンション。Queryは一方の入力から、Key/Valueは別のコンテキストから生成します。 |
| `ScaledDotProductAttention` | Scaled Dot-Product Attentionの実装。アテンション計算の基盤となるモジュールです。 |
| `AttentionBase` | アテンションモジュールの抽象基底クラス。Query/Key/Value投影と位置エンコーディングの共通処理を提供します。 |

### 位置エンコーディング

| クラス | 説明 |
|--------|------|
| `RotaryPositionalEncoding` | Rotary Position Embedding (RoPE) の実装。Query/Keyテンソルに回転変換を適用して位置情報をエンコードします。 |
| `ALiBiPositionalEncoding` | Attention with Linear Biases (ALiBi) の実装。アテンションスコアに線形バイアスを加算して位置情報をエンコードします。 |

---

## APIリファレンス

### SelfAttention

シングルヘッド・セルフアテンションモジュール。入力シーケンス自身に対してアテンションを計算します。Query/Key/Valueすべてが同一の入力から生成されます。

#### コンストラクタ

```csharp
public SelfAttention(
    int embeddingDimension,
    int? queryDimension = null,
    int? valueDimension = null,
    float dropoutProbability = 0.0f,
    IPositionalEncoding? positionalEncoding = null,
    bool useBias = false)
```

| パラメータ | 型 | 説明 |
|-----------|-----|------|
| `embeddingDimension` | `int` | 入力/出力の埋め込み次元数 |
| `queryDimension` | `int?` | Query/Keyの内部次元数（nullの場合は`embeddingDimension`を使用） |
| `valueDimension` | `int?` | Valueの内部次元数（nullの場合は`embeddingDimension`を使用） |
| `dropoutProbability` | `float` | アテンション重みのドロップアウト確率（0.0で無効化） |
| `positionalEncoding` | `IPositionalEncoding?` | 位置エンコーディング（nullで無効化） |
| `useBias` | `bool` | 線形変換にバイアスを使用するかどうか |

#### メソッド

##### forward

```csharp
public Tensor forward(Tensor input, Tensor? mask = null, int positionOffset = 0)
```

セルフアテンションを計算します。

| パラメータ | 型 | 説明 |
|-----------|-----|------|
| `input` | `Tensor` | 入力テンソル `[batch, seqLen, embeddingDim]` |
| `mask` | `Tensor?` | アテンションマスク `[seqLen, seqLen]` または `[batch, seqLen, seqLen]`。Trueの位置がマスクされます。 |
| `positionOffset` | `int` | 位置オフセット（RoPE使用時のキャッシュ対応用） |

**戻り値**: アテンション出力 `[batch, seqLen, embeddingDim]`

---

### CrossAttention

シングルヘッド・クロスアテンションモジュール。Encoder-Decoderアーキテクチャなどで使用されます。

#### コンストラクタ

```csharp
public CrossAttention(
    int embeddingDimension,
    int? queryDimension = null,
    int? valueDimension = null,
    float dropoutProbability = 0.0f,
    IPositionalEncoding? positionalEncoding = null,
    bool useBias = false)
```

パラメータは`SelfAttention`と同一です。

#### メソッド

##### forward

```csharp
public Tensor forward(
    Tensor queryInput,
    Tensor keyInput,
    Tensor valueInput,
    Tensor? mask = null,
    int positionOffset = 0)
```

クロスアテンションを計算します。

| パラメータ | 型 | 説明 |
|-----------|-----|------|
| `queryInput` | `Tensor` | Query生成元の入力テンソル `[batch, queryLen, embeddingDim]` |
| `keyInput` | `Tensor` | Key生成元の入力テンソル `[batch, keyLen, embeddingDim]` |
| `valueInput` | `Tensor` | Value生成元の入力テンソル `[batch, keyLen, embeddingDim]` |
| `mask` | `Tensor?` | アテンションマスク（オプション） |
| `positionOffset` | `int` | 位置オフセット（RoPE使用時のキャッシュ対応用） |

**戻り値**: アテンション出力 `[batch, queryLen, embeddingDim]`

---

### ScaledDotProductAttention

Scaled Dot-Product Attentionの実装。

**アテンション計算式**: `Attention(Q, K, V) = softmax(QK^T / √d_k + mask + bias) V`

#### コンストラクタ

```csharp
public ScaledDotProductAttention(int dimension, float dropoutProbability = 0.0f)
```

| パラメータ | 型 | 説明 |
|-----------|-----|------|
| `dimension` | `int` | Key/Queryの次元数（スケーリング係数の計算に使用） |
| `dropoutProbability` | `float` | ドロップアウト確率（0.0で無効化） |

#### メソッド

##### forward

```csharp
public Tensor forward(
    Tensor query,
    Tensor key,
    Tensor value,
    Tensor? mask = null,
    Tensor? scoreBias = null)
```

| パラメータ | 型 | 説明 |
|-----------|-----|------|
| `query` | `Tensor` | Queryテンソル `[batch, queryLen, dim]` |
| `key` | `Tensor` | Keyテンソル `[batch, keyLen, dim]` |
| `value` | `Tensor` | Valueテンソル `[batch, keyLen, valueDim]` |
| `mask` | `Tensor?` | アテンションマスク。Trueの位置がマスクされます（-1e9が加算されます）。 |
| `scoreBias` | `Tensor?` | スコアバイアス `[queryLen, keyLen]`。ALiBi等で使用。 |

**戻り値**: アテンション出力 `[batch, queryLen, valueDim]`

---

### RotaryPositionalEncoding

Rotary Position Embedding (RoPE) の実装。Query/Keyテンソルに回転変換を適用して位置情報をエンコードします。

**参考論文**: [RoFormer: Enhanced Transformer with Rotary Position Embedding](https://arxiv.org/abs/2104.09864)

#### コンストラクタ

```csharp
public RotaryPositionalEncoding(
    int dimension,
    int maxSequenceLength = 2048,
    float baseFrequency = 10000.0f,
    Device? device = null,
    ScalarType dtype = ScalarType.Float32)
```

| パラメータ | 型 | 説明 |
|-----------|-----|------|
| `dimension` | `int` | 埋め込み次元数（偶数である必要があります） |
| `maxSequenceLength` | `int` | サポートする最大シーケンス長 |
| `baseFrequency` | `float` | 基底周波数 |
| `device` | `Device?` | テンソルを配置するデバイス（デフォルト: CPU） |
| `dtype` | `ScalarType` | テンソルのデータ型（デフォルト: Float32） |

#### プロパティ

| プロパティ | 型 | 説明 |
|-----------|-----|------|
| `EncodingType` | `PositionalEncodingType` | `QueryKeyTransform`を返します |

#### メソッド

##### ApplyToQueryKey

```csharp
public (Tensor query, Tensor key) ApplyToQueryKey(
    Tensor query,
    Tensor key,
    int positionOffset = 0)
```

Query/Keyテンソルに位置エンコーディングを適用します。

| パラメータ | 型 | 説明 |
|-----------|-----|------|
| `query` | `Tensor` | Queryテンソル `[batch, seqLen, dim]` |
| `key` | `Tensor` | Keyテンソル `[batch, seqLen, dim]` |
| `positionOffset` | `int` | 位置オフセット（キャッシュ使用時など） |

**戻り値**: 位置エンコーディング適用後の`(Query, Key)`タプル

**例外**: `ArgumentOutOfRangeException` - 最大長を超えた位置が入力された場合

---

### ALiBiPositionalEncoding

Attention with Linear Biases (ALiBi) の実装。アテンションスコアに線形バイアスを加算して位置情報をエンコードします。

**参考論文**: [Train Short, Test Long: Attention with Linear Biases Enables Input Length Extrapolation](https://arxiv.org/abs/2108.12409)

#### コンストラクタ

```csharp
public ALiBiPositionalEncoding(
    int maxLength = 2048,
    float slope = 1.0f,
    Device? device = null,
    ScalarType dtype = ScalarType.Float32)
```

| パラメータ | 型 | 説明 |
|-----------|-----|------|
| `maxLength` | `int` | サポートする最大シーケンス長 |
| `slope` | `float` | バイアスの傾き（シングルヘッド用）。マルチヘッドでは各ヘッドで異なる値を使用します。 |
| `device` | `Device?` | テンソルを配置するデバイス（デフォルト: CPU） |
| `dtype` | `ScalarType` | テンソルのデータ型（デフォルト: Float32） |

#### プロパティ

| プロパティ | 型 | 説明 |
|-----------|-----|------|
| `EncodingType` | `PositionalEncodingType` | `ScoreBias`を返します |

#### メソッド

##### GetScoreBias

```csharp
public Tensor GetScoreBias(
    int queryLength,
    int keyLength,
    Device device,
    ScalarType dtype)
```

アテンションスコアに加算するバイアステンソルを取得します。

| パラメータ | 型 | 説明 |
|-----------|-----|------|
| `queryLength` | `int` | Queryのシーケンス長 |
| `keyLength` | `int` | Keyのシーケンス長 |
| `device` | `Device` | デバイス |
| `dtype` | `ScalarType` | データ型 |

**戻り値**: スコアバイアステンソル `[queryLength, keyLength]`

**例外**: `ArgumentOutOfRangeException` - queryLengthまたはkeyLengthが最大長を超えた場合

##### ApplyToQueryKey

```csharp
public (Tensor query, Tensor key) ApplyToQueryKey(
    Tensor query,
    Tensor key,
    int positionOffset = 0)
```

**例外**: `NotSupportedException` - ALiBiはScoreBiasタイプのため、このメソッドはサポートされていません。

---

### IPositionalEncoding

位置エンコーディングのインターフェース。

#### プロパティ

| プロパティ | 型 | 説明 |
|-----------|-----|------|
| `EncodingType` | `PositionalEncodingType` | 位置エンコーディングの適用タイプ |

#### メソッド

| メソッド | 説明 |
|---------|------|
| `ApplyToQueryKey(Tensor, Tensor, int)` | Query/Keyテンソルに位置エンコーディングを適用（`QueryKeyTransform`タイプ用） |
| `GetScoreBias(int, int, Device, ScalarType)` | スコアバイアステンソルを取得（`ScoreBias`タイプ用） |

---

### PositionalEncodingType

位置エンコーディングの適用タイプを表す列挙型。

| 値 | 説明 |
|----|------|
| `QueryKeyTransform` | Query/Keyテンソルに変換を適用するタイプ（RoPEなど） |
| `ScoreBias` | アテンションスコアにバイアスを加算するタイプ（ALiBiなど） |

---

## ユーティリティ

### MLCoreModuleUtility

アテンション機構を簡単に生成するためのファクトリメソッドを提供する静的クラスです。

#### セルフアテンション生成メソッド

| メソッド | 説明 |
|---------|------|
| `CreateSelfAttention(...)` | 標準的なセルフアテンション機構を生成 |
| `CreateSelfAttentionWithRope(...)` | RoPEを使用したセルフアテンション機構を生成 |
| `CreateSelfAttentionWithAlibi(...)` | ALiBiを使用したセルフアテンション機構を生成 |

#### クロスアテンション生成メソッド

| メソッド | 説明 |
|---------|------|
| `CreateCrossAttention(...)` | 標準的なクロスアテンション機構を生成 |
| `CreateCrossAttentionWithRope(...)` | RoPEを使用したクロスアテンション機構を生成 |
| `CreateCrossAttentionWithAlibi(...)` | ALiBiを使用したクロスアテンション機構を生成 |

---

## 関連ドキュメント

- [MLModelCodec README](../MLModelCodec/README.md) - ONNXエンコーディングライブラリ

---

## ライセンス

zlib License

Copyright (c) 2025-2026 Sinoa
