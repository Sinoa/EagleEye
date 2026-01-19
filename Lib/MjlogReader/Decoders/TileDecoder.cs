// zlib License
// 
// Copyright (c) 2026 Sinoa
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

using Foxtamp.MjlogReader.Models;

namespace Foxtamp.MjlogReader.Decoders;

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
    /// カンマ区切りの牌ID文字列から複数のTileをデコード
    /// </summary>
    /// <param name="tileIds">カンマ区切りの牌ID文字列</param>
    /// <returns>Tileのリスト</returns>
    public static List<Tile> DecodeMultiple(string tileIds)
    {
        if (string.IsNullOrWhiteSpace(tileIds))
        {
            return [];
        }

        var tiles = new List<Tile>();
        foreach (var idStr in tileIds.Split(','))
        {
            if (int.TryParse(idStr.Trim(), out var id))
            {
                tiles.Add(Decode(id));
            }
        }

        return tiles;
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
    /// 天鳳形式の牌IDからタイプID（0-33）を取得
    /// </summary>
    public static int GetTileTypeId(int tileId)
    {
        if (tileId < 0 || tileId > 135)
        {
            throw new ArgumentOutOfRangeException(nameof(tileId), $"Invalid tile ID: {tileId}");
        }

        if (tileId < 36)
        {
            return tileId / 4; // 萬子: 0-8
        }

        if (tileId < 72)
        {
            return (tileId - 36) / 4 + 9; // 筒子: 9-17
        }

        if (tileId < 108)
        {
            return (tileId - 72) / 4 + 18; // 索子: 18-26
        }

        return (tileId - 108) / 4 + 27; // 字牌: 27-33
    }
}