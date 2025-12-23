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
/// - 0-8: 1萬〜9萬
/// - 9: 赤5萬
/// - 10-18: 1筒〜9筒
/// - 19: 赤5筒
/// - 20-28: 1索〜9索
/// - 29: 赤5索
/// - 30-36: 東南西北白發中
/// - 37-43: 属性トークン（萬子、筒子、索子、風牌、三元牌、数牌、字牌、赤牌）
/// </remarks>
public enum TileTokenId
{
    // 萬子 (0-8)
    Man1 = 0,
    Man2 = 1,
    Man3 = 2,
    Man4 = 3,
    Man5 = 4,
    Man6 = 5,
    Man7 = 6,
    Man8 = 7,
    Man9 = 8,
    RedMan5 = 9,

    // 筒子 (10-18)
    Pin1 = 10,
    Pin2 = 11,
    Pin3 = 12,
    Pin4 = 13,
    Pin5 = 14,
    Pin6 = 15,
    Pin7 = 16,
    Pin8 = 17,
    Pin9 = 18,
    RedPin5 = 19,

    // 索子 (20-28)
    Sou1 = 20,
    Sou2 = 21,
    Sou3 = 22,
    Sou4 = 23,
    Sou5 = 24,
    Sou6 = 25,
    Sou7 = 26,
    Sou8 = 27,
    Sou9 = 28,
    RedSou5 = 29,

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