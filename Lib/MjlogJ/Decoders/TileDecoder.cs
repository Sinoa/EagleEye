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

using MjlogJ.Models;

namespace MjlogJ.Decoders;

/// <summary>
/// 天鳳形式の牌IDをデコードするクラス
/// </summary>
/// <remarks>
/// 天鳳の牌ID（0-135）の構造:
/// - 0-35: 萬子 (1m-9m × 4枚)
/// - 36-71: 筒子 (1p-9p × 4枚)
/// - 72-107: 索子 (1s-9s × 4枚)
/// - 108-135: 字牌 (東南西北白發中 × 4枚)
/// 赤ドラ: 各スートの5の牌のうち、0番目（ID: 16, 52, 88）が赤ドラ
/// </remarks>
public static class TileDecoder
{
    /// <summary>
    /// 天鳳形式の牌IDからTileオブジェクトを生成
    /// </summary>
    /// <param name="tileId">天鳳形式の牌ID（0-135）</param>
    /// <returns>Tileオブジェクト</returns>
    public static Tile Decode(int tileId)
    {
        if (tileId < 0 || tileId > 135)
        {
            throw new ArgumentOutOfRangeException(nameof(tileId), $"Invalid tile ID: {tileId}");
        }

        TileSuit suit;
        int number;
        var isRedDora = false;

        if (tileId < 36)
        {
            // 萬子
            suit = TileSuit.Man;
            number = tileId / 4 + 1;
            // 5m の 0 番目 (ID: 16) が赤ドラ
            isRedDora = tileId == 16;
        }
        else if (tileId < 72)
        {
            // 筒子
            suit = TileSuit.Pin;
            number = (tileId - 36) / 4 + 1;
            // 5p の 0 番目 (ID: 52) が赤ドラ
            isRedDora = tileId == 52;
        }
        else if (tileId < 108)
        {
            // 索子
            suit = TileSuit.Sou;
            number = (tileId - 72) / 4 + 1;
            // 5s の 0 番目 (ID: 88) が赤ドラ
            isRedDora = tileId == 88;
        }
        else
        {
            // 字牌
            suit = TileSuit.Honor;
            number = (tileId - 108) / 4 + 1;
        }

        return new Tile(suit, number, isRedDora, tileId);
    }

    /// <summary>
    /// TileオブジェクトからベースとなるタイプID（0-33）を取得
    /// </summary>
    /// <remarks>
    /// タイプID:
    /// 0-8: 萬子 1-9
    /// 9-17: 筒子 1-9
    /// 18-26: 索子 1-9
    /// 27-33: 字牌 東南西北白發中
    /// </remarks>
    public static int GetTileTypeId(Tile tile)
    {
        return tile.TileTypeId;
    }

    /// <summary>
    /// タイプIDとインデックスから天鳳形式の牌IDを生成
    /// </summary>
    /// <param name="typeId">タイプID（0-33）</param>
    /// <param name="index">同じ牌の中でのインデックス（0-3）</param>
    /// <returns>天鳳形式の牌ID</returns>
    public static int Encode(int typeId, int index)
    {
        if (typeId < 0 || typeId > 33)
        {
            throw new ArgumentOutOfRangeException(nameof(typeId), $"Invalid type ID: {typeId}");
        }

        if (index < 0 || index > 3)
        {
            throw new ArgumentOutOfRangeException(nameof(index), $"Invalid index: {index}");
        }

        return typeId * 4 + index;
    }

    /// <summary>
    /// カンマ区切りの牌ID文字列をデコード
    /// </summary>
    /// <param name="tileIds">カンマ区切りの牌ID文字列</param>
    /// <returns>Tileのリスト</returns>
    public static List<Tile> DecodeMultiple(string tileIds)
    {
        if (string.IsNullOrEmpty(tileIds))
        {
            return [];
        }

        return tileIds.Split(',')
            .Select(s => int.TryParse(s.Trim(), out var id) ? id : -1)
            .Where(id => id >= 0 && id <= 135)
            .Select(Decode)
            .ToList();
    }

    /// <summary>
    /// 牌ID配列をデコード
    /// </summary>
    /// <param name="tileIds">牌IDの配列</param>
    /// <returns>Tileのリスト</returns>
    public static List<Tile> DecodeMultiple(int[] tileIds)
    {
        return tileIds
            .Where(id => id >= 0 && id <= 135)
            .Select(Decode)
            .ToList();
    }

    /// <summary>
    /// 牌が赤ドラかどうかを判定
    /// </summary>
    public static bool IsRedDora(int tileId)
    {
        return tileId == 16 || tileId == 52 || tileId == 88;
    }
}