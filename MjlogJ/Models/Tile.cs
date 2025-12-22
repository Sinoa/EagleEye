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

namespace MjlogJ.Models;

/// <summary>
/// 牌の種別
/// </summary>
public enum TileSuit
{
    /// <summary>萬子</summary>
    Man,
    /// <summary>筒子</summary>
    Pin,
    /// <summary>索子</summary>
    Sou,
    /// <summary>字牌</summary>
    Honor
}

/// <summary>
/// 字牌の種類
/// </summary>
public enum HonorType
{
    /// <summary>東</summary>
    East = 1,
    /// <summary>南</summary>
    South = 2,
    /// <summary>西</summary>
    West = 3,
    /// <summary>北</summary>
    North = 4,
    /// <summary>白</summary>
    White = 5,
    /// <summary>發</summary>
    Green = 6,
    /// <summary>中</summary>
    Red = 7
}

/// <summary>
/// 麻雀牌を表すレコード
/// </summary>
/// <param name="Suit">牌の種別</param>
/// <param name="Number">数字（数牌:1-9、字牌:1-7）</param>
/// <param name="IsRedDora">赤ドラかどうか</param>
/// <param name="OriginalId">天鳳形式の元ID（0-135）</param>
public record Tile(TileSuit Suit, int Number, bool IsRedDora, int OriginalId)
{
    /// <summary>
    /// 牌の表示用文字列を取得
    /// </summary>
    public string DisplayName => Suit switch
    {
        TileSuit.Man => $"{Number}m{(IsRedDora ? "r" : "")}",
        TileSuit.Pin => $"{Number}p{(IsRedDora ? "r" : "")}",
        TileSuit.Sou => $"{Number}s{(IsRedDora ? "r" : "")}",
        TileSuit.Honor => ((HonorType)Number) switch
        {
            HonorType.East => "東",
            HonorType.South => "南",
            HonorType.West => "西",
            HonorType.North => "北",
            HonorType.White => "白",
            HonorType.Green => "發",
            HonorType.Red => "中",
            _ => $"?{Number}"
        },
        _ => "?"
    };

    /// <summary>
    /// 牌の種類ID（赤ドラを区別しない同一牌判定用）
    /// </summary>
    public int TileTypeId => Suit switch
    {
        TileSuit.Man => Number - 1,
        TileSuit.Pin => 9 + Number - 1,
        TileSuit.Sou => 18 + Number - 1,
        TileSuit.Honor => 27 + Number - 1,
        _ => -1
    };
}

