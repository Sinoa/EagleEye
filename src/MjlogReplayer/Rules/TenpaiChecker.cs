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

namespace Foxtamp.MjlogReplayer.Rules;

/// <summary>
/// 聴牌判定を行う静的クラス
/// </summary>
public static class TenpaiChecker
{
    /// <summary>
    /// 手牌が聴牌かどうかを判定する
    /// </summary>
    /// <param name="handTiles">閉じた手牌（副露分を除く）</param>
    /// <param name="meldCount">副露数</param>
    /// <returns>聴牌の場合true</returns>
    public static bool IsTenpai(IReadOnlyList<Tile> handTiles, int meldCount)
    {
        return GetWaitingTileTypeIds(handTiles, meldCount).Count > 0;
    }

    /// <summary>
    /// 待ち牌のTileTypeIdリストを取得する
    /// </summary>
    /// <param name="handTiles">閉じた手牌（副露分を除く）</param>
    /// <param name="meldCount">副露数</param>
    /// <returns>待ち牌のTileTypeIdリスト</returns>
    public static IReadOnlyList<int> GetWaitingTileTypeIds(IReadOnlyList<Tile> handTiles, int meldCount)
    {
        var expectedCount = (4 - meldCount) * 3 + 1;
        if (handTiles.Count != expectedCount)
        {
            return [];
        }

        var counts = new int[34];
        foreach (var tile in handTiles)
        {
            counts[tile.TileTypeId]++;
        }

        return GetWaitingTileTypeIdsFromCounts(counts, meldCount);
    }

    /// <summary>
    /// カウント配列から聴牌判定を行う（内部用）
    /// </summary>
    internal static bool IsTenpaiFromCounts(int[] counts, int meldCount)
    {
        return GetWaitingTileTypeIdsFromCounts(counts, meldCount).Count > 0;
    }

    /// <summary>
    /// カウント配列から待ち牌を取得する（内部用）
    /// </summary>
    internal static IReadOnlyList<int> GetWaitingTileTypeIdsFromCounts(int[] counts, int meldCount)
    {
        var waits = new List<int>();
        for (var i = 0; i < 34; i++)
        {
            if (counts[i] >= 4)
            {
                continue;
            }

            counts[i]++;
            if (AgariChecker.IsAgariFromCounts(counts, meldCount))
            {
                waits.Add(i);
            }

            counts[i]--;
        }

        return waits;
    }
}