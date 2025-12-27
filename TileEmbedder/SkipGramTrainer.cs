// zlib License
// 
// Copyright (c) 2025 Sinoa
// 
// This software is provided 'as-is', without any express or implied
// warranty. In no event will the authors be held liable for any damages
// arising from the use of this software.
// 
// Permission is granted to anyone to use this software for any purpose,
// including commercial applications, and to alter it and redistribute it
// freely, subject to the following restrictions:
// 
// 1. The origin of this software must not be misrepresented; you must not
// claim that you wrote the original software. If you use this software
// in a product, an acknowledgment in the product documentation would be
// appreciated but is not required.
// 
// 2. Altered source versions must be plainly marked as such, and must not be
// misrepresented as being the original software.
// 
// 3. This notice may not be removed or altered from any source
// distribution.

using System.Globalization;
using System.Text.Json;
using MLModelUtility.Formats.Safetensors;
using MLModelUtility.Models;

namespace TileEmbedder;

/// <summary>
/// Skip-gramモデルの学習設定
/// </summary>
public class SkipGramTrainingOptions
{
    /// <summary>埋め込みベクトルの次元数</summary>
    public int EmbeddingDim { get; set; } = TrainingConstants.DefaultEmbeddingDim;

    /// <summary>学習エポック数</summary>
    public int Epochs { get; set; } = TrainingConstants.DefaultEpochs;

    /// <summary>ネガティブサンプル数</summary>
    public int NegativeSamples { get; set; } = TrainingConstants.DefaultNegativeSamples;

    /// <summary>学習率</summary>
    public float LearningRate { get; set; } = TrainingConstants.DefaultLearningRate;

    /// <summary>乱数シード（再現性のため）</summary>
    public int? RandomSeed { get; set; }

    /// <summary>エポック毎のコールバック</summary>
    public Action<int, float>? OnEpochComplete { get; set; }

    /// <summary>チェックポイント保存パス（nullの場合は保存しない）</summary>
    public string? CheckpointPath { get; set; }

    /// <summary>チェックポイント保存間隔（エポック数）</summary>
    public int? CheckpointInterval { get; set; }
}

/// <summary>
/// ファイルからのロード結果（トレーナーとハイパーパラメータを含む）
/// </summary>
public class SkipGramLoadResult
{
    /// <summary>ロードされたトレーナー</summary>
    public required SkipGramTrainer Trainer { get; init; }

    /// <summary>ファイルに保存されていたハイパーパラメータ（存在しない場合はnull）</summary>
    public TrainingHyperparameters? Hyperparameters { get; init; }

    /// <summary>ハイパーパラメータが存在するか</summary>
    public bool HasHyperparameters => Hyperparameters != null;
}

/// <summary>
/// Skip-gramモデルのトレーナー
/// </summary>
public class SkipGramTrainer
{
    private readonly int _vocabSize;
    private readonly int _embeddingDim;
    private readonly Random _random;

    private float[,] _inputWeights; // 入力層重み（語彙 × 次元）
    private float[,] _outputWeights; // 出力層重み（語彙 × 次元）

    /// <summary>
    /// 学習済み埋め込みベクトルを取得
    /// </summary>
    public float[,] Embeddings => _inputWeights;

    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="vocabSize">語彙サイズ</param>
    /// <param name="embeddingDim">埋め込み次元数</param>
    /// <param name="randomSeed">乱数シード</param>
    public SkipGramTrainer(
        int vocabSize = TrainingConstants.TotalVocabularySize,
        int embeddingDim = TrainingConstants.DefaultEmbeddingDim,
        int? randomSeed = null)
    {
        _vocabSize = vocabSize;
        _embeddingDim = embeddingDim;
        _random = randomSeed.HasValue ? new Random(randomSeed.Value) : new Random();

        _inputWeights = new float[vocabSize, embeddingDim];
        _outputWeights = new float[vocabSize, embeddingDim];

        InitializeWeights();
    }

    /// <summary>
    /// Safetensors形式から学習済み埋め込みベクトルをロードしてトレーナーを作成
    /// </summary>
    /// <param name="filePath">Safetensorsファイルのパス</param>
    /// <param name="randomSeed">乱数シード（追加学習用）</param>
    /// <param name="tensorName">テンソル名（デフォルト: "tile_embeddings"）</param>
    /// <returns>埋め込みがロードされたトレーナー</returns>
    public static SkipGramTrainer LoadFromSafetensors(
        string filePath,
        int? randomSeed = null,
        string tensorName = "tile_embeddings")
    {
        return LoadFromSafetensorsWithHyperparameters(filePath, randomSeed, tensorName).Trainer;
    }

    /// <summary>
    /// Safetensors形式から学習済み埋め込みベクトルとハイパーパラメータをロード
    /// </summary>
    /// <param name="filePath">Safetensorsファイルのパス</param>
    /// <param name="randomSeed">乱数シード（追加学習用）</param>
    /// <param name="tensorName">テンソル名（デフォルト: "tile_embeddings"）</param>
    /// <returns>トレーナーとハイパーパラメータを含むロード結果</returns>
    public static SkipGramLoadResult LoadFromSafetensorsWithHyperparameters(
        string filePath,
        int? randomSeed = null,
        string tensorName = "tile_embeddings")
    {
        var handler = new SafetensorsFormatHandler();
        using var collection = handler.ReadTensorsFromFile(filePath);

        // メタデータから情報を取得
        var metadata = collection.Metadata;
        if (metadata == null || !metadata.TryGetValue("vocab_size", out var vocabSizeStr) || !metadata.TryGetValue("embedding_dim", out var embeddingDimStr))
        {
            throw new InvalidDataException("Safetensors file missing required metadata (vocab_size, embedding_dim)");
        }

        int vocabSize = int.Parse(vocabSizeStr);
        int embeddingDim = int.Parse(embeddingDimStr);

        // テンソルデータを取得
        if (!collection.TryGetTensor(tensorName, out var tensor) || tensor == null)
        {
            throw new InvalidDataException($"Tensor '{tensorName}' not found in Safetensors file");
        }

        if (tensor.Info.DataType != TensorDataType.Float32)
        {
            throw new InvalidDataException($"Expected Float32 tensor, got {tensor.Info.DataType}");
        }

        // トレーナーを作成（初期化をスキップして後で重みを設定）
        var trainer = new SkipGramTrainer(vocabSize, embeddingDim, randomSeed);

        // 埋め込みベクトルを読み込み（入力層重みとして設定）
        var flatData = tensor.GetDataAs<float>();
        int index = 0;
        for (int i = 0; i < vocabSize; i++)
        {
            for (int d = 0; d < embeddingDim; d++)
            {
                trainer._inputWeights[i, d] = flatData[index++];
            }
        }

        // 出力層重みは0初期化（追加学習を想定）
        for (int i = 0; i < vocabSize; i++)
        {
            for (int d = 0; d < embeddingDim; d++)
            {
                trainer._outputWeights[i, d] = 0f;
            }
        }

        // ハイパーパラメータをメタデータから読み込み
        var hyperparameters = ParseHyperparametersFromSafetensors(metadata, embeddingDim);

        return new SkipGramLoadResult
        {
            Trainer = trainer,
            Hyperparameters = hyperparameters
        };
    }

    /// <summary>
    /// Safetensorsメタデータからハイパーパラメータを解析
    /// </summary>
    private static TrainingHyperparameters? ParseHyperparametersFromSafetensors(
        IReadOnlyDictionary<string, string> metadata,
        int embeddingDim)
    {
        // hp_プレフィックス付きのハイパーパラメータがあるか確認
        if (!metadata.TryGetValue("hp_epochs", out var epochsStr))
        {
            return null; // ハイパーパラメータが保存されていない
        }

        var hyperparameters = new TrainingHyperparameters
        {
            EmbeddingDim = embeddingDim
        };

        if (int.TryParse(epochsStr, out var epochs))
        {
            hyperparameters.Epochs = epochs;
        }

        if (metadata.TryGetValue("hp_negative_samples", out var negSamplesStr) && int.TryParse(negSamplesStr, out var negSamples))
        {
            hyperparameters.NegativeSamples = negSamples;
        }

        if (metadata.TryGetValue("hp_learning_rate", out var lrStr) && float.TryParse(lrStr, NumberStyles.Float, CultureInfo.InvariantCulture, out var lr))
        {
            hyperparameters.LearningRate = lr;
        }

        if (metadata.TryGetValue("hp_random_seed", out var seedStr) && int.TryParse(seedStr, out var seed))
        {
            hyperparameters.RandomSeed = seed;
        }

        if (metadata.TryGetValue("hp_created_at", out var createdAtStr)
            && DateTime.TryParse(createdAtStr, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var createdAt))
        {
            hyperparameters.CreatedAt = createdAt;
        }

        if (metadata.TryGetValue("hp_updated_at", out var updatedAtStr)
            && DateTime.TryParse(updatedAtStr, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var updatedAt))
        {
            hyperparameters.UpdatedAt = updatedAt;
        }

        return hyperparameters;
    }

    /// <summary>
    /// JSON形式から学習済み埋め込みベクトルをロードしてトレーナーを作成
    /// </summary>
    /// <param name="filePath">JSONファイルのパス</param>
    /// <param name="randomSeed">乱数シード（追加学習用）</param>
    /// <returns>埋め込みがロードされたトレーナー</returns>
    public static SkipGramTrainer LoadFromJson(string filePath, int? randomSeed = null)
    {
        return LoadFromJsonWithHyperparameters(filePath, randomSeed).Trainer;
    }

    /// <summary>
    /// JSON形式から学習済み埋め込みベクトルとハイパーパラメータをロード
    /// </summary>
    /// <param name="filePath">JSONファイルのパス</param>
    /// <param name="randomSeed">乱数シード（追加学習用）</param>
    /// <returns>トレーナーとハイパーパラメータを含むロード結果</returns>
    public static SkipGramLoadResult LoadFromJsonWithHyperparameters(string filePath, int? randomSeed = null)
    {
        var json = File.ReadAllText(filePath);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        // メタデータを取得
        int vocabSize = root.GetProperty("vocab_size").GetInt32();
        int embeddingDim = root.GetProperty("embedding_dim").GetInt32();

        // トレーナーを作成
        var trainer = new SkipGramTrainer(vocabSize, embeddingDim, randomSeed);

        // 埋め込みベクトルを読み込み
        var embeddings = root.GetProperty("embeddings");
        foreach (var kvp in embeddings.EnumerateObject())
        {
            var tokenName = kvp.Name;
            var vector = kvp.Value;

            // TileTokenIdに変換
            if (Enum.TryParse<TileTokenId>(tokenName, out var tokenId))
            {
                int tokenIndex = (int)tokenId;
                int d = 0;
                foreach (var value in vector.EnumerateArray())
                {
                    if (d < embeddingDim)
                    {
                        trainer._inputWeights[tokenIndex, d] = value.GetSingle();
                        d++;
                    }
                }
            }
        }

        // 出力層重みは0初期化
        for (int i = 0; i < vocabSize; i++)
        {
            for (int d = 0; d < embeddingDim; d++)
            {
                trainer._outputWeights[i, d] = 0f;
            }
        }

        // ハイパーパラメータを読み込み
        var hyperparameters = ParseHyperparametersFromJson(root, embeddingDim);

        return new SkipGramLoadResult
        {
            Trainer = trainer,
            Hyperparameters = hyperparameters
        };
    }

    /// <summary>
    /// JSONからハイパーパラメータを解析
    /// </summary>
    private static TrainingHyperparameters? ParseHyperparametersFromJson(JsonElement root, int embeddingDim)
    {
        if (!root.TryGetProperty("hyperparameters", out var hpElement))
        {
            return null; // ハイパーパラメータが保存されていない
        }

        var hyperparameters = new TrainingHyperparameters
        {
            EmbeddingDim = embeddingDim
        };

        if (hpElement.TryGetProperty("epochs", out var epochsElement))
        {
            hyperparameters.Epochs = epochsElement.GetInt32();
        }

        if (hpElement.TryGetProperty("negative_samples", out var negSamplesElement))
        {
            hyperparameters.NegativeSamples = negSamplesElement.GetInt32();
        }

        if (hpElement.TryGetProperty("learning_rate", out var lrElement))
        {
            hyperparameters.LearningRate = lrElement.GetSingle();
        }

        if (hpElement.TryGetProperty("random_seed", out var seedElement) && seedElement.ValueKind != JsonValueKind.Null)
        {
            hyperparameters.RandomSeed = seedElement.GetInt32();
        }

        if (hpElement.TryGetProperty("created_at", out var createdAtElement))
        {
            var createdAtStr = createdAtElement.GetString();
            if (createdAtStr != null && DateTime.TryParse(createdAtStr, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var createdAt))
            {
                hyperparameters.CreatedAt = createdAt;
            }
        }

        if (hpElement.TryGetProperty("updated_at", out var updatedAtElement))
        {
            var updatedAtStr = updatedAtElement.GetString();
            if (updatedAtStr != null && DateTime.TryParse(updatedAtStr, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var updatedAt))
            {
                hyperparameters.UpdatedAt = updatedAt;
            }
        }

        return hyperparameters;
    }

    /// <summary>
    /// 学習済みモデルの重みを保存（バイナリ形式）
    /// </summary>
    /// <param name="filePath">保存先ファイルパス</param>
    /// <remarks>
    /// 入力層重みと出力層重みの両方を保存するため、学習の完全な再開が可能。
    /// Safetensors形式は推論用の埋め込みベクトルのみを保存する。
    /// </remarks>
    public void SaveWeights(string filePath)
    {
        using var writer = new BinaryWriter(File.Create(filePath));

        // ヘッダー
        writer.Write(0x57475053); // マジックナンバー "SPGW" (Skip-Gram Weights)
        writer.Write(1); // バージョン
        writer.Write(_vocabSize);
        writer.Write(_embeddingDim);

        // 入力層重みを保存
        for (int i = 0; i < _vocabSize; i++)
        {
            for (int j = 0; j < _embeddingDim; j++)
            {
                writer.Write(_inputWeights[i, j]);
            }
        }

        // 出力層重みを保存
        for (int i = 0; i < _vocabSize; i++)
        {
            for (int j = 0; j < _embeddingDim; j++)
            {
                writer.Write(_outputWeights[i, j]);
            }
        }
    }

    /// <summary>
    /// バイナリ形式の重みファイルからトレーナーをロード
    /// </summary>
    /// <param name="filePath">重みファイルのパス</param>
    /// <param name="randomSeed">乱数シード（追加学習用）</param>
    /// <returns>重みがロードされたトレーナー</returns>
    /// <remarks>
    /// SaveWeightsで保存した完全な学習状態（入力層・出力層両方）を復元。
    /// 学習の完全な再開が可能。
    /// </remarks>
    public static SkipGramTrainer LoadFromWeights(string filePath, int? randomSeed = null)
    {
        using var reader = new BinaryReader(File.OpenRead(filePath));

        // ヘッダー読み込み
        int magic = reader.ReadInt32();
        if (magic != 0x57475053) // "SPGW"
            throw new InvalidDataException("Invalid weights file format");

        int version = reader.ReadInt32();
        if (version != 1)
            throw new InvalidDataException($"Unsupported weights file version: {version}");

        int vocabSize = reader.ReadInt32();
        int embeddingDim = reader.ReadInt32();

        // トレーナーを作成
        var trainer = new SkipGramTrainer(vocabSize, embeddingDim, randomSeed);

        // 入力層重みを読み込み
        for (int i = 0; i < vocabSize; i++)
        {
            for (int j = 0; j < embeddingDim; j++)
            {
                trainer._inputWeights[i, j] = reader.ReadSingle();
            }
        }

        // 出力層重みを読み込み
        for (int i = 0; i < vocabSize; i++)
        {
            for (int j = 0; j < embeddingDim; j++)
            {
                trainer._outputWeights[i, j] = reader.ReadSingle();
            }
        }

        return trainer;
    }

    /// <summary>
    /// 重みを初期化
    /// </summary>
    /// <remarks>
    /// Word2Vec/Skip-gramの標準的な初期化手法を使用。
    /// 入力層: U(-0.25/dim, 0.25/dim) の一様分布
    /// 出力層: 0初期化（負例サンプリングで学習初期の確率を0.5に保つため）
    /// </remarks>
    private void InitializeWeights()
    {
        float scale = 0.5f / _embeddingDim;

        for (int i = 0; i < _vocabSize; i++)
        {
            for (int j = 0; j < _embeddingDim; j++)
            {
                _inputWeights[i, j] = (float)(_random.NextDouble() - 0.5) * scale;
                _outputWeights[i, j] = 0f;
            }
        }
    }

    /// <summary>
    /// シグモイド関数
    /// </summary>
    private static float Sigmoid(float x)
    {
        if (x > 20f) return 1f;
        if (x < -20f) return 0f;
        return 1f / (1f + MathF.Exp(-x));
    }

    /// <summary>
    /// 内積を計算
    /// </summary>
    private float DotProduct(int inputToken, int outputToken)
    {
        float dot = 0f;
        for (int d = 0; d < _embeddingDim; d++)
        {
            dot += _inputWeights[inputToken, d] * _outputWeights[outputToken, d];
        }

        return dot;
    }

    /// <summary>
    /// 共起行列から学習を実行
    /// </summary>
    /// <param name="matrix">共起行列</param>
    /// <param name="options">学習オプション</param>
    public void Train(CooccurrenceMatrix matrix, SkipGramTrainingOptions? options = null)
    {
        options ??= new SkipGramTrainingOptions();

        float learningRate = options.LearningRate;
        int negativeSamples = options.NegativeSamples;
        int epochs = options.Epochs;

        // 学習ペアを生成
        var pairs = matrix.GenerateTrainingPairs();
        if (pairs.Count == 0)
        {
            throw new InvalidOperationException("No training pairs available. Build base matrix first.");
        }

        // ネガティブサンプリング用の確率分布（ユニグラム分布^0.75）
        var unigramDistribution = BuildUnigramDistribution(matrix);

        // 勾配用の一時バッファ
        var gradient = new float[_embeddingDim];

        for (int epoch = 0; epoch < epochs; epoch++)
        {
            // ペアをシャッフル
            ShuffleList(pairs);

            float totalLoss = 0f;
            int pairCount = 0;

            foreach (var (centerToken, contextToken) in pairs)
            {
                // 正例の学習
                float loss = TrainPair(centerToken, contextToken, true, learningRate, gradient);
                totalLoss += loss;

                // 負例の学習
                for (int n = 0; n < negativeSamples; n++)
                {
                    int negativeToken = SampleNegative(unigramDistribution, centerToken);
                    loss = TrainPair(centerToken, negativeToken, false, learningRate, gradient);
                    totalLoss += loss;
                }

                pairCount++;
            }

            float avgLoss = totalLoss / (pairCount * (1 + negativeSamples));
            options.OnEpochComplete?.Invoke(epoch + 1, avgLoss);

            // チェックポイント保存
            if (options.CheckpointPath != null && options.CheckpointInterval.HasValue && (epoch + 1) % options.CheckpointInterval.Value == 0)
            {
                string checkpointFile = $"{options.CheckpointPath}.epoch{epoch + 1}.bin";
                SaveWeights(checkpointFile);
            }
        }
    }

    /// <summary>
    /// 1ペアの学習を実行
    /// </summary>
    private float TrainPair(int centerToken, int contextToken, bool isPositive, float learningRate, float[] gradient)
    {
        // 内積を計算
        float dot = DotProduct(centerToken, contextToken);

        // シグモイド出力
        float sigmoid = Sigmoid(dot);

        // ターゲット（正例: 1, 負例: 0）
        float target = isPositive ? 1f : 0f;

        // 誤差
        float error = target - sigmoid;

        // 損失（交差エントロピー）
        float loss = isPositive
            ? -MathF.Log(sigmoid + 1e-10f)
            : -MathF.Log(1f - sigmoid + 1e-10f);

        // 勾配を計算して更新
        for (int d = 0; d < _embeddingDim; d++)
        {
            gradient[d] = error * _outputWeights[contextToken, d];
        }

        // 出力層重みを更新
        for (int d = 0; d < _embeddingDim; d++)
        {
            _outputWeights[contextToken, d] += learningRate * error * _inputWeights[centerToken, d];
        }

        // 入力層重みを更新
        for (int d = 0; d < _embeddingDim; d++)
        {
            _inputWeights[centerToken, d] += learningRate * gradient[d];
        }

        return loss;
    }

    /// <summary>
    /// ユニグラム分布を構築（ネガティブサンプリング用）
    /// </summary>
    private int[] BuildUnigramDistribution(CooccurrenceMatrix matrix, int tableSize = 100000)
    {
        var counts = new long[_vocabSize];

        // 各トークンの出現頻度をカウント
        foreach (var (token1, token2, count) in matrix.EnumerateCooccurrences())
        {
            counts[token1] += count;
            counts[token2] += count;
        }

        // 頻度^0.75 で正規化
        var powers = new double[_vocabSize];
        double totalPower = 0;
        for (int i = 0; i < _vocabSize; i++)
        {
            powers[i] = Math.Pow(counts[i] + 1, 0.75); // +1 でスムージング
            totalPower += powers[i];
        }

        // サンプリングテーブルを構築
        var table = new int[tableSize];
        int tableIndex = 0;
        double cumulative = 0;

        for (int i = 0; i < _vocabSize; i++)
        {
            cumulative += powers[i] / totalPower;
            int targetIndex = (int)(cumulative * tableSize);

            while (tableIndex < targetIndex && tableIndex < tableSize)
            {
                table[tableIndex] = i;
                tableIndex++;
            }
        }

        // 残りを埋める
        while (tableIndex < tableSize)
        {
            table[tableIndex] = _vocabSize - 1;
            tableIndex++;
        }

        return table;
    }

    /// <summary>
    /// ネガティブサンプルを取得
    /// </summary>
    private int SampleNegative(int[] unigramTable, int excludeToken)
    {
        int sample;
        do
        {
            sample = unigramTable[_random.Next(unigramTable.Length)];
        } while (sample == excludeToken);

        return sample;
    }

    /// <summary>
    /// リストをシャッフル（Fisher-Yates）
    /// </summary>
    private void ShuffleList<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = _random.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    /// <summary>
    /// 特定のトークンの埋め込みベクトルを取得
    /// </summary>
    /// <param name="tokenId">トークンID</param>
    /// <returns>埋め込みベクトル</returns>
    public float[] GetEmbedding(int tokenId)
    {
        if (tokenId < 0 || tokenId >= _vocabSize)
            throw new ArgumentOutOfRangeException(nameof(tokenId));

        var embedding = new float[_embeddingDim];
        for (int d = 0; d < _embeddingDim; d++)
        {
            embedding[d] = _inputWeights[tokenId, d];
        }

        return embedding;
    }

    /// <summary>
    /// 特定のトークンの埋め込みベクトルを取得（TileTokenId版）
    /// </summary>
    public float[] GetEmbedding(TileTokenId tokenId)
    {
        return GetEmbedding((int)tokenId);
    }

    /// <summary>
    /// 全ての埋め込みベクトルを1次元配列として取得
    /// </summary>
    /// <returns>フラットな埋め込み配列（語彙 × 次元）</returns>
    public float[] GetFlatEmbeddings()
    {
        var flat = new float[_vocabSize * _embeddingDim];
        int index = 0;

        for (int i = 0; i < _vocabSize; i++)
        {
            for (int d = 0; d < _embeddingDim; d++)
            {
                flat[index++] = _inputWeights[i, d];
            }
        }

        return flat;
    }

    /// <summary>
    /// 2つのトークン間のコサイン類似度を計算
    /// </summary>
    public float CosineSimilarity(int token1, int token2)
    {
        float dot = 0f, norm1 = 0f, norm2 = 0f;

        for (int d = 0; d < _embeddingDim; d++)
        {
            float v1 = _inputWeights[token1, d];
            float v2 = _inputWeights[token2, d];
            dot += v1 * v2;
            norm1 += v1 * v1;
            norm2 += v2 * v2;
        }

        if (norm1 == 0 || norm2 == 0) return 0f;
        return dot / (MathF.Sqrt(norm1) * MathF.Sqrt(norm2));
    }

    /// <summary>
    /// 2つのトークン間のコサイン類似度を計算（TileTokenId版）
    /// </summary>
    public float CosineSimilarity(TileTokenId token1, TileTokenId token2)
    {
        return CosineSimilarity((int)token1, (int)token2);
    }
}