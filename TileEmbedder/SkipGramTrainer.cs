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
    /// 重みを初期化（小さなランダム値）
    /// </summary>
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