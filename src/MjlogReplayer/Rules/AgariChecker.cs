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
/// 和了判定を行う静的クラス
/// </summary>
public static class AgariChecker
{
    private static readonly int[] KokushiTileTypeIds = [0, 8, 9, 17, 18, 26, 27, 28, 29, 30, 31, 32, 33];

    /// <summary>
    /// 手牌が和了形かどうかを判定する
    /// </summary>
    /// <param name="handTiles">閉じた手牌（副露分を除く）</param>
    /// <param name="meldCount">副露数</param>
    /// <returns>和了形の場合true</returns>
    public static bool IsAgari(IReadOnlyList<Tile> handTiles, int meldCount)
    {
        var expectedCount = (4 - meldCount) * 3 + 2;
        if (handTiles.Count != expectedCount)
        {
            return false;
        }

        var counts = new int[34];
        foreach (var tile in handTiles)
        {
            counts[tile.TileTypeId]++;
        }

        return IsAgariFromCounts(counts, meldCount);
    }

    /// <summary>
    /// カウント配列から和了判定を行う（内部用）
    /// </summary>
    internal static bool IsAgariFromCounts(int[] counts, int meldCount)
    {
        return TryStandardForm(counts)
               || (meldCount == 0 && TryChiitoitsu(counts))
               || (meldCount == 0 && TryKokushimusou(counts));
    }

    /// <summary>
    /// 通常形（4面子+1雀頭）の判定
    /// </summary>
    private static bool TryStandardForm(int[] counts)
    {
        for (var i = 0; i < 34; i++)
        {
            if (counts[i] < 2)
            {
                continue;
            }

            counts[i] -= 2;
            var result = TryRemoveMentsu(counts);
            counts[i] += 2;
            if (result)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 残りの牌を面子に再帰分解する
    /// </summary>
    private static bool TryRemoveMentsu(int[] counts)
    {
        var first = -1;
        for (var i = 0; i < 34; i++)
        {
            if (counts[i] > 0)
            {
                first = i;
                break;
            }
        }

        if (first == -1)
        {
            return true;
        }

        // 刻子として除去
        if (counts[first] >= 3)
        {
            counts[first] -= 3;
            var result = TryRemoveMentsu(counts);
            counts[first] += 3;
            if (result)
            {
                return true;
            }
        }

        // 順子として除去（数牌のみ、同一Suit内）
        if (first < 27 && first % 9 <= 6 && counts[first + 1] > 0 && counts[first + 2] > 0)
        {
            counts[first]--;
            counts[first + 1]--;
            counts[first + 2]--;
            var result = TryRemoveMentsu(counts);
            counts[first]++;
            counts[first + 1]++;
            counts[first + 2]++;
            if (result)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 七対子の判定
    /// </summary>
    private static bool TryChiitoitsu(int[] counts)
    {
        var pairs = 0;
        for (var i = 0; i < 34; i++)
        {
            if (counts[i] == 2)
            {
                pairs++;
            }
            else if (counts[i] != 0)
            {
                return false;
            }
        }

        return pairs == 7;
    }

    /// <summary>
    /// 国士無双の判定
    /// </summary>
    private static bool TryKokushimusou(int[] counts)
    {
        for (var i = 0; i < 34; i++)
        {
            var isKokushi = Array.IndexOf(KokushiTileTypeIds, i) >= 0;
            if (isKokushi)
            {
                if (counts[i] < 1)
                {
                    return false;
                }
            }
            else
            {
                if (counts[i] > 0)
                {
                    return false;
                }
            }
        }

        return true;
    }
}