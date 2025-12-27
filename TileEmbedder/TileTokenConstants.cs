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
/// 麻雀牌のトークンID
/// </summary>
/// <remarks>
/// トークンIDの構成:
/// - 0: 赤5萬, 1-9: 1萬〜9萬（一の位0が赤牌）
/// - 10: 赤5筒, 11-19: 1筒〜9筒（一の位0が赤牌）
/// - 20: 赤5索, 21-29: 1索〜9索（一の位0が赤牌）
/// - 30-36: 東南西北白發中
/// - 37-44: 属性トークン（萬子、筒子、索子、風牌、三元牌、数牌、字牌、赤牌）
/// </remarks>
public enum TileTokenId
{
    // 萬子 (0-9) - 一の位0が赤5萬
    RedMan5 = 0,
    Man1 = 1,
    Man2 = 2,
    Man3 = 3,
    Man4 = 4,
    Man5 = 5,
    Man6 = 6,
    Man7 = 7,
    Man8 = 8,
    Man9 = 9,

    // 筒子 (10-19) - 一の位0が赤5筒
    RedPin5 = 10,
    Pin1 = 11,
    Pin2 = 12,
    Pin3 = 13,
    Pin4 = 14,
    Pin5 = 15,
    Pin6 = 16,
    Pin7 = 17,
    Pin8 = 18,
    Pin9 = 19,

    // 索子 (20-29) - 一の位0が赤5索
    RedSou5 = 20,
    Sou1 = 21,
    Sou2 = 22,
    Sou3 = 23,
    Sou4 = 24,
    Sou5 = 25,
    Sou6 = 26,
    Sou7 = 27,
    Sou8 = 28,
    Sou9 = 29,

    // 字牌 (30-36)
    East = 30,
    South = 31,
    West = 32,
    North = 33,
    White = 34,
    Green = 35,
    Red = 36,

    // 属性トークン (37-44)
    AttrMan = 37, // 萬子属性
    AttrPin = 38, // 筒子属性
    AttrSou = 39, // 索子属性
    AttrWind = 40, // 風牌属性
    AttrDragon = 41, // 三元牌属性
    AttrNumber = 42, // 数牌属性
    AttrHonor = 43, // 字牌属性
    AttrRed = 44 // 赤牌属性
}

/// <summary>
/// Skip-gram学習のハイパーパラメータ定数
/// </summary>
public static class TrainingConstants
{
    /// <summary>埋め込みベクトルの次元数（デフォルト: 4）</summary>
    public const int DefaultEmbeddingDim = 4;

    /// <summary>学習エポック数（デフォルト: 100）</summary>
    public const int DefaultEpochs = 100;

    /// <summary>ネガティブサンプル数（デフォルト: 10）</summary>
    public const int DefaultNegativeSamples = 10;

    /// <summary>学習率（デフォルト: 0.025）</summary>
    public const float DefaultLearningRate = 0.025f;

    /// <summary>語彙サイズ（実牌トークン数: 37）</summary>
    public const int RealTileVocabularySize = 37;

    /// <summary>属性トークンを含む全語彙サイズ: 45</summary>
    public const int TotalVocabularySize = 45;

    /// <summary>順子近接共起のウィンドウサイズ（デフォルト: 2）</summary>
    public const int DefaultWindowSize = 2;
}

/// <summary>
/// 学習に使用されたハイパーパラメータ情報
/// </summary>
public class TrainingHyperparameters
{
    /// <summary>埋め込みベクトルの次元数</summary>
    public int EmbeddingDim { get; set; }

    /// <summary>学習エポック数</summary>
    public int Epochs { get; set; }

    /// <summary>ネガティブサンプル数</summary>
    public int NegativeSamples { get; set; }

    /// <summary>学習率</summary>
    public float LearningRate { get; set; }

    /// <summary>乱数シード（nullの場合はランダム）</summary>
    public int? RandomSeed { get; set; }

    /// <summary>生成日時</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// メタデータ用の辞書に変換
    /// </summary>
    public Dictionary<string, string> ToDictionary()
    {
        var dict = new Dictionary<string, string>
        {
            ["embedding_dim"] = EmbeddingDim.ToString(),
            ["epochs"] = Epochs.ToString(),
            ["negative_samples"] = NegativeSamples.ToString(),
            ["learning_rate"] = LearningRate.ToString("G"),
            ["created_at"] = CreatedAt.ToString("o")
        };

        if (RandomSeed.HasValue)
        {
            dict["random_seed"] = RandomSeed.Value.ToString();
        }

        return dict;
    }

    /// <summary>
    /// サマリー文字列を生成
    /// </summary>
    public string ToSummaryString()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("=== Training Hyperparameters Summary ===");
        sb.AppendLine($"Embedding Dimension : {EmbeddingDim}");
        sb.AppendLine($"Epochs              : {Epochs}");
        sb.AppendLine($"Negative Samples    : {NegativeSamples}");
        sb.AppendLine($"Learning Rate       : {LearningRate:G}");
        sb.AppendLine($"Random Seed         : {(RandomSeed.HasValue ? RandomSeed.Value.ToString() : "Auto (not specified)")}");
        sb.AppendLine($"Created At          : {CreatedAt:yyyy-MM-dd HH:mm:ss} UTC");
        sb.AppendLine("========================================");
        return sb.ToString();
    }
}