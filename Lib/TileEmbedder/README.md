# TileEmbedder

麻雀牌の埋め込みベクトルを生成するためのライブラリです。Skip-gramアーキテクチャを使用し、ルールベースの共起関係と実際の牌譜データから学習します。

## 概要

このライブラリは以下の機能を提供します：

- 麻雀牌37種（通常牌30種 + 赤牌3種 + 字牌7種）+ 属性トークン8種のトークン定義
- ルールベースの共起行列構築
- MjlogJ牌譜からの和了手牌に基づく共起データ抽出
- Skip-gram（負例サンプリング）による埋め込みベクトル学習
- Safetensors / JSON形式でのエクスポート

## クイックスタート

### 基本的な使用方法（ルールベースのみ）

```csharp
using TileEmbedder;

// 共起行列を作成し、ルールベースの共起を構築
var matrix = new CooccurrenceMatrix();
matrix.BuildBaseMatrix();

// Skip-gramトレーナーを作成
var trainer = new SkipGramTrainer();

// 学習を実行
trainer.Train(matrix, new SkipGramTrainingOptions
{
    OnEpochComplete = (epoch, loss) =>
    {
        Console.WriteLine($"Epoch {epoch}: Loss = {loss:F6}");
    }
});

// 埋め込みベクトルをエクスポート
EmbeddingExporter.ExportToSafetensors(trainer, "tile_embeddings.safetensors");
EmbeddingExporter.ExportToJson(trainer, "tile_embeddings.json");
```

### 牌譜データを使った追加学習

```csharp
using MjlogJ;
using TileEmbedder;

// 共起行列を作成
var matrix = new CooccurrenceMatrix();
matrix.BuildBaseMatrix();

// 牌譜ディレクトリから和了手牌の共起を抽出
await foreach (var record in MjlogReader.LoadManyAsync("./mjlogs/"))
{
    MjlogCooccurrenceExtractor.ExtractFromGameRecord(record, matrix);
}

// 学習を実行
var trainer = new SkipGramTrainer();
trainer.Train(matrix);

// エクスポート
EmbeddingExporter.ExportToSafetensors(trainer, "tile_embeddings.safetensors");
```

## 学習済みモデルのロードと再開

### Safetensors形式からロード（推論・追加学習用）

既に学習済みの埋め込みベクトルをロードして、推論や追加学習に使用できます：

```csharp
// 学習済み埋め込みベクトルをロード
var trainer = SkipGramTrainer.LoadFromSafetensors("tile_embeddings.safetensors");

// ロードしたモデルで類似度を計算
float similarity = trainer.CosineSimilarity(TileTokenId.Man1, TileTokenId.Man2);
Console.WriteLine($"1萬と2萬の類似度: {similarity:F4}");

// 新しいデータで追加学習（ファインチューニング）
var matrix = new CooccurrenceMatrix();
matrix.BuildBaseMatrix();
// ... 新しいデータを追加 ...

trainer.Train(matrix, new SkipGramTrainingOptions
{
    Epochs = 50,
    LearningRate = 0.01f  // 小さい学習率で微調整
});
```

### JSON形式からロード

```csharp
var trainer = SkipGramTrainer.LoadFromJson("tile_embeddings.json");
```

### 完全な学習状態の保存と再開

入力層・出力層の両方の重みを保存することで、学習を完全に再開できます：

```csharp
// 学習中の重みを保存（入力層・出力層両方）
trainer.SaveWeights("training_state.bin");

// 完全な状態から再開
var resumedTrainer = SkipGramTrainer.LoadFromWeights("training_state.bin");
resumedTrainer.Train(matrix, new SkipGramTrainingOptions { Epochs = 100 });
```

### チェックポイント機能

長時間の学習で定期的に重みを自動保存：

```csharp
var trainer = new SkipGramTrainer();
trainer.Train(matrix, new SkipGramTrainingOptions
{
    Epochs = 1000,
    CheckpointPath = "checkpoints/model",
    CheckpointInterval = 100,  // 100エポックごとに保存
    OnEpochComplete = (epoch, loss) =>
    {
        Console.WriteLine($"Epoch {epoch}: Loss = {loss:F6}");
    }
});
// → checkpoints/model.epoch100.bin
//   checkpoints/model.epoch200.bin
//   ... が生成される
```

学習の中断後、最後のチェックポイントから再開：

```csharp
var trainer = SkipGramTrainer.LoadFromWeights("checkpoints/model.epoch500.bin");
trainer.Train(matrix, new SkipGramTrainingOptions 
{ 
    Epochs = 500,  // 残り500エポック
    CheckpointPath = "checkpoints/model",
    CheckpointInterval = 100
});
```

### 形式の使い分け

| 形式 | 用途 | 入力層重み | 出力層重み | メソッド |
|------|------|-----------|-----------|---------|
| **Safetensors** | 推論・配布・ファインチューニング | ✓ | ✗ | `LoadFromSafetensors()` |
| **JSON** | デバッグ・可読性確認 | ✓ | ✗ | `LoadFromJson()` |
| **バイナリ (.bin)** | 学習の完全な再開 | ✓ | ✓ | `LoadFromWeights()` / `SaveWeights()` |

## トークン定義

### 実牌トークン（0-36）

トークンIDは一の位が0の時に赤牌であることが視覚的にわかりやすくなるよう設計されています。

| ID | トークン | 説明      |
|----|----------|---------|
| 0 | RedMan5 | 赤5萬     |
| 1-9 | Man1-Man9 | 1萬～9萬   |
| 10 | RedPin5 | 赤5筒     |
| 11-19 | Pin1-Pin9 | 1筒～9筒   |
| 20 | RedSou5 | 赤5索     |
| 21-29 | Sou1-Sou9 | 1索～9索   |
| 30-36 | East-Red | 東南西北白發中 |

### 属性トークン（37-44）

| ID | トークン | 説明 |
|----|----------|------|
| 37 | AttrMan | 萬子属性 |
| 38 | AttrPin | 筒子属性 |
| 39 | AttrSou | 索子属性 |
| 40 | AttrWind | 風牌属性 |
| 41 | AttrDragon | 三元牌属性 |
| 42 | AttrNumber | 数牌属性 |
| 43 | AttrHonor | 字牌属性 |
| 44 | AttrRed | 赤牌属性 |

## ハイパーパラメータ

`TrainingConstants` クラスでデフォルト値が定義されています：

| パラメータ | デフォルト値 | 説明 |
|-----------|-------------|------|
| `DefaultEmbeddingDim` | 4 | 埋め込みベクトルの次元数 |
| `DefaultEpochs` | 100 | 学習エポック数 |
| `DefaultNegativeSamples` | 10 | ネガティブサンプル数 |
| `DefaultLearningRate` | 0.025 | 学習率 |
| `DefaultWindowSize` | 2 | 順子近接共起のウィンドウサイズ |

### カスタム設定

```csharp
var trainer = new SkipGramTrainer(
    vocabSize: TrainingConstants.TotalVocabularySize,
    embeddingDim: 8,  // 8次元に変更
    randomSeed: 42    // 再現性のためのシード
);

trainer.Train(matrix, new SkipGramTrainingOptions
{
    Epochs = 200,
    NegativeSamples = 15,
    LearningRate = 0.01f
});
```

## 共起ルール

### ベース共起行列

`BuildBaseMatrix()` で以下のルールベース共起が構築されます：

1. **同種牌の共起**: 萬子同士、筒子同士、索子同士、風牌同士、三元牌同士で相互に+1
2. **順子近接共起**: 数牌でウィンドウサイズ2の隣接牌に+1
   - 例: 3萬は1萬,2萬,4萬,5萬と共起
   - 赤5は456だけでなく345,567とも共起（順子形成可能なすべての組み合わせ）
3. **属性トークンとの共起**: 各牌は対応する属性トークンと+1
   - 例: 1萬 ⇔ (萬子属性, 数牌属性)
   - 例: 赤5萬 ⇔ (萬子属性, 数牌属性, 赤牌属性)
   - 例: 東 ⇔ (風牌属性, 字牌属性)

### 牌譜からの追加共起

`MjlogCooccurrenceExtractor` で和了時の以下の牌から共起ペアを抽出：

- 手牌（`AgariResult.Hand`）
- 副露牌（`AgariResult.Melds[].Tiles`）
- 和了牌（`AgariResult.WinningTile`）

## エクスポート形式

### Safetensors

```csharp
EmbeddingExporter.ExportToSafetensors(trainer, "embeddings.safetensors");
```

メタデータに `vocab_size`, `embedding_dim`, `token_mapping` が含まれます。

### JSON

```csharp
EmbeddingExporter.ExportToJson(trainer, "embeddings.json", indented: true);
```

### 類似度行列CSV

```csharp
EmbeddingExporter.ExportSimilarityMatrixToCsv(trainer, "similarity.csv");
```

## 類似度の確認

```csharp
// 2つのトークン間のコサイン類似度を計算
float similarity = trainer.CosineSimilarity(TileTokenId.Man1, TileTokenId.Man2);
Console.WriteLine($"1萬と2萬の類似度: {similarity:F4}");

// 特定のトークンの埋め込みベクトルを取得
float[] embedding = trainer.GetEmbedding(TileTokenId.East);
```

## ライセンス

zlib License

## 関連プロジェクト

- [TileEmbedderCli](../TileEmbedderCli/README.md) - このライブラリを使用するCLIツール
- [MjlogJ](../MjlogJ/README.md) - 天鳳牌譜読み込みライブラリ
- [MLModelUtility](../MLModelUtility/README.md) - AIモデル入出力ライブラリ（Safetensors対応）

