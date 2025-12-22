// zlib License
// 
// Copyright (c) 2025 Sinoa
// 
// This software is provided ‘as-is’, without any express or implied
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

using MjlogConverter.Models;
using MjlogConverter.Models.ComputedData;

namespace MjlogConverter.Analyzers;

/// <summary>
/// 待ち牌を計算するアナライザー
/// </summary>
/// <remarks>
/// 将来的に各ステップでの待ち牌計算にも対応できるよう、
/// 静的メソッドとして実装
/// </remarks>
public static class WaitingTileAnalyzer
{
    /// <summary>
    /// 手牌から待ち牌を計算
    /// </summary>
    /// <param name="hand">手牌（13枚または副露を含めて3n+1枚）</param>
    /// <param name="melds">副露のリスト</param>
    /// <param name="playerId">プレイヤーID</param>
    /// <param name="sequence">計算時点のシーケンス番号</param>
    /// <param name="visibleTiles">見えている牌（残り枚数計算用）</param>
    /// <returns>待ち牌情報、テンパイでない場合はnull</returns>
    public static WaitingTilesInfo? Analyze(
        IReadOnlyList<Tile> hand,
        IReadOnlyList<MeldInfo> melds,
        int playerId,
        int sequence,
        IReadOnlyList<Tile>? visibleTiles = null)
    {
        // 手牌をタイプID別にカウント
        var handCounts = new int[34];
        foreach (var tile in hand)
        {
            handCounts[tile.TileTypeId]++;
        }

        // 待ち牌を探す
        var waitingTypeIds = FindWaitingTiles(handCounts, melds.Count);

        if (waitingTypeIds.Count == 0)
        {
            return null;
        }

        // 待ち牌をTileオブジェクトに変換
        var waitingTiles = waitingTypeIds
            .Select(CreateTileFromTypeId)
            .ToList();

        // 残り枚数を計算
        int remainingCount;
        if (visibleTiles != null)
        {
            var visibleCounts = new int[34];
            foreach (var tile in visibleTiles)
            {
                visibleCounts[tile.TileTypeId]++;
            }

            foreach (var tile in hand)
            {
                visibleCounts[tile.TileTypeId]++;
            }

            foreach (var meld in melds)
            {
                foreach (var tile in meld.Tiles)
                {
                    visibleCounts[tile.TileTypeId]++;
                }
            }

            remainingCount = waitingTypeIds.Sum(typeId => 4 - visibleCounts[typeId]);
        }
        else
        {
            remainingCount = waitingTypeIds.Sum(typeId => 4 - handCounts[typeId]);
        }

        return new WaitingTilesInfo
        {
            PlayerId = playerId,
            WaitingTiles = waitingTiles,
            RemainingCount = remainingCount,
            AtSequence = sequence,
            HandAtCalculation = hand.ToList(),
            MeldsAtCalculation = melds.ToList()
        };
    }

    /// <summary>
    /// 待ち牌のタイプIDを探す
    /// </summary>
    private static List<int> FindWaitingTiles(int[] handCounts, int meldCount)
    {
        var waitingTiles = new List<int>();
        var effectiveHandSize = handCounts.Sum() + (meldCount * 3);

        // 通常形：手牌が13枚（+ 副露3枚×n）
        if (effectiveHandSize != 13)
        {
            return waitingTiles;
        }

        // 各牌を追加してみて和了形になるか判定
        for (int typeId = 0; typeId < 34; typeId++)
        {
            if (handCounts[typeId] >= 4) continue; // 4枚使用済み

            handCounts[typeId]++;
            if (IsCompleteHand(handCounts, meldCount))
            {
                waitingTiles.Add(typeId);
            }

            handCounts[typeId]--;
        }

        return waitingTiles;
    }

    /// <summary>
    /// 和了形かどうかを判定
    /// </summary>
    private static bool IsCompleteHand(int[] handCounts, int meldCount)
    {
        // 七対子チェック
        if (meldCount == 0 && IsSevenPairs(handCounts))
        {
            return true;
        }

        // 国士無双チェック
        if (meldCount == 0 && IsThirteenOrphans(handCounts))
        {
            return true;
        }

        // 通常形（4面子1雀頭）チェック
        return IsNormalComplete(handCounts, meldCount);
    }

    /// <summary>
    /// 七対子かどうか
    /// </summary>
    private static bool IsSevenPairs(int[] handCounts)
    {
        var pairs = 0;
        foreach (var count in handCounts)
        {
            if (count == 2) pairs++;
            else if (count != 0) return false;
        }

        return pairs == 7;
    }

    /// <summary>
    /// 国士無双かどうか
    /// </summary>
    private static bool IsThirteenOrphans(int[] handCounts)
    {
        // 么九牌のタイプID
        int[] terminals = [0, 8, 9, 17, 18, 26, 27, 28, 29, 30, 31, 32, 33];

        var hasPair = false;
        foreach (var typeId in terminals)
        {
            if (handCounts[typeId] == 0) return false;
            if (handCounts[typeId] == 2)
            {
                if (hasPair) return false;
                hasPair = true;
            }
            else if (handCounts[typeId] > 2) return false;
        }

        // 么九牌以外がないことを確認
        for (int i = 0; i < 34; i++)
        {
            if (!terminals.Contains(i) && handCounts[i] > 0) return false;
        }

        return hasPair;
    }

    /// <summary>
    /// 通常形（4面子1雀頭）かどうか
    /// </summary>
    private static bool IsNormalComplete(int[] handCounts, int meldCount)
    {
        var counts = (int[])handCounts.Clone();
        var requiredMentsu = 4 - meldCount;

        // 雀頭を選ぶ
        for (int headId = 0; headId < 34; headId++)
        {
            if (counts[headId] < 2) continue;

            counts[headId] -= 2;

            if (CanFormMentsu(counts, requiredMentsu))
            {
                counts[headId] += 2;
                return true;
            }

            counts[headId] += 2;
        }

        return false;
    }

    /// <summary>
    /// 指定数の面子を構成できるか
    /// </summary>
    private static bool CanFormMentsu(int[] counts, int required)
    {
        if (required == 0)
        {
            return counts.All(c => c == 0);
        }

        // 最初の牌から処理
        for (int i = 0; i < 34; i++)
        {
            if (counts[i] == 0) continue;

            // 刻子を試す
            if (counts[i] >= 3)
            {
                counts[i] -= 3;
                if (CanFormMentsu(counts, required - 1))
                {
                    counts[i] += 3;
                    return true;
                }

                counts[i] += 3;
            }

            // 順子を試す（数牌のみ、7以下）
            if (i < 27 && (i % 9) <= 6)
            {
                if (counts[i] >= 1 && counts[i + 1] >= 1 && counts[i + 2] >= 1)
                {
                    counts[i]--;
                    counts[i + 1]--;
                    counts[i + 2]--;
                    if (CanFormMentsu(counts, required - 1))
                    {
                        counts[i]++;
                        counts[i + 1]++;
                        counts[i + 2]++;
                        return true;
                    }

                    counts[i]++;
                    counts[i + 1]++;
                    counts[i + 2]++;
                }
            }

            // この牌を処理できなかった
            return false;
        }

        return false;
    }

    /// <summary>
    /// タイプIDからTileオブジェクトを生成（代表牌）
    /// </summary>
    private static Tile CreateTileFromTypeId(int typeId)
    {
        TileSuit suit;
        int number;

        if (typeId < 9)
        {
            suit = TileSuit.Man;
            number = typeId + 1;
        }
        else if (typeId < 18)
        {
            suit = TileSuit.Pin;
            number = typeId - 9 + 1;
        }
        else if (typeId < 27)
        {
            suit = TileSuit.Sou;
            number = typeId - 18 + 1;
        }
        else
        {
            suit = TileSuit.Honor;
            number = typeId - 27 + 1;
        }

        // 代表牌として最初のID（赤ドラでない方）を使用
        var originalId = typeId * 4;
        // 5の牌は赤ドラ判定
        var isRedDora = false;

        return new Tile(suit, number, isRedDora, originalId);
    }
}