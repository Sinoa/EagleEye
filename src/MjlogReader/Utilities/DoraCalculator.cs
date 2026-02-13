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

namespace Foxtamp.MjlogReader.Utilities;

/// <summary>
/// ドラ表示牌からドラ牌を算出するユーティリティ
/// </summary>
public static class DoraCalculator
{
    /// <summary>
    /// ドラ表示牌からドラ牌のTileTypeIdを返す
    /// </summary>
    /// <param name="indicator">ドラ表示牌</param>
    /// <param name="isThreePlayer">三麻モード（萬子は1mと9mのみ）</param>
    /// <returns>ドラ牌のTileTypeId（0-33）</returns>
    public static int GetDoraTileTypeId(Tile indicator, bool isThreePlayer = false)
    {
        if (indicator.Suit != TileSuit.Honor)
        {
            // 三麻の萬子: 1m→9m, 9m→1m（2m-8mが存在しない）
            if (isThreePlayer && indicator.Suit == TileSuit.Man)
            {
                return indicator.Number == 1 ? 8 : 0;
            }

            // 数牌: 1→2→...→9→1 のサイクル
            var suitBase = indicator.Suit switch
            {
                TileSuit.Man => 0,
                TileSuit.Pin => 9,
                TileSuit.Sou => 18,
                _ => throw new ArgumentException($"不正な牌種別です: {indicator.Suit}", nameof(indicator))
            };
            var position = indicator.Number - 1;
            var doraPosition = (position + 1) % 9;
            return suitBase + doraPosition;
        }

        if (indicator.Number <= 4)
        {
            // 風牌: 東→南→西→北→東 のサイクル
            var position = indicator.Number - 1;
            var doraPosition = (position + 1) % 4;
            return 27 + doraPosition;
        }

        // 三元牌: 白→發→中→白 のサイクル
        {
            var position = indicator.Number - 5;
            var doraPosition = (position + 1) % 3;
            return 31 + doraPosition;
        }
    }

    /// <summary>
    /// 指定牌がドラ表示牌群に対してドラかどうかを判定する
    /// </summary>
    /// <param name="tile">判定対象の牌</param>
    /// <param name="doraIndicators">ドラ表示牌のリスト</param>
    /// <param name="isThreePlayer">三麻モード（萬子は1mと9mのみ）</param>
    /// <returns>ドラの場合true</returns>
    public static bool IsDora(Tile tile, IReadOnlyList<Tile> doraIndicators, bool isThreePlayer = false)
    {
        var tileTypeId = tile.TileTypeId;
        for (var i = 0; i < doraIndicators.Count; i++)
        {
            if (GetDoraTileTypeId(doraIndicators[i], isThreePlayer) == tileTypeId)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 牌列に対するドラフラグ配列を生成する
    /// </summary>
    /// <param name="tiles">判定対象の牌列</param>
    /// <param name="doraIndicators">ドラ表示牌のリスト</param>
    /// <param name="isThreePlayer">三麻モード（萬子は1mと9mのみ）</param>
    /// <returns>各牌がドラかどうかを示すフラグ配列</returns>
    public static bool[] GetDoraFlags(IReadOnlyList<Tile> tiles, IReadOnlyList<Tile> doraIndicators, bool isThreePlayer = false)
    {
        var flags = new bool[tiles.Count];
        for (var i = 0; i < tiles.Count; i++)
        {
            flags[i] = IsDora(tiles[i], doraIndicators, isThreePlayer);
        }

        return flags;
    }
}
