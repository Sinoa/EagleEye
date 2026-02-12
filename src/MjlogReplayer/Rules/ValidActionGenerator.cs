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
using Foxtamp.MjlogReplayer.Models;

namespace Foxtamp.MjlogReplayer.Rules;

/// <summary>
/// 合法アクションを生成する静的クラス
/// </summary>
public static class ValidActionGenerator
{
    /// <summary>
    /// ツモ番（牌を引いた後）の合法アクションを取得する
    /// </summary>
    /// <param name="state">現在の試合状態</param>
    /// <param name="playerId">ツモ番のプレイヤーID</param>
    /// <returns>合法アクションのフラグ合成</returns>
    public static GameActionType GetValidActionsOnDraw(GameState state, int playerId)
    {
        var player = state.GetPlayer(playerId);
        var hand = player.Hand;
        var meldCount = player.Melds.Count;
        var result = GameActionType.None;

        // ツモ和了判定
        if (AgariChecker.IsAgari(hand, meldCount))
        {
            result |= GameActionType.Tsumo;
        }

        // リーチ判定
        var canRiichi = false;
        if (!player.IsReach && IsClosedHand(player) && player.Score >= 1000 && state.RemainingTileCount >= 4)
        {
            var counts = ToCountArray(hand);
            for (var i = 0; i < 34; i++)
            {
                if (counts[i] <= 0)
                {
                    continue;
                }

                counts[i]--;
                if (TenpaiChecker.IsTenpaiFromCounts(counts, meldCount))
                {
                    canRiichi = true;
                    counts[i]++;
                    break;
                }

                counts[i]++;
            }
        }

        // 打牌は常に有効、リーチ可能なら追加で有効
        result |= GameActionType.Discard;
        if (canRiichi)
        {
            result |= GameActionType.Riichi;
        }

        // 暗槓（リーチ中は不可）
        if (!player.IsReach)
        {
            var counts = ToCountArray(hand);
            for (var i = 0; i < 34; i++)
            {
                if (counts[i] >= 4)
                {
                    result |= GameActionType.AnKan;
                    break;
                }
            }
        }

        // 加槓
        if (player.Melds.Count > 0)
        {
            var handTypeIds = new HashSet<int>(hand.Select(t => t.TileTypeId));
            for (var i = 0; i < player.Melds.Count; i++)
            {
                var meld = player.Melds[i];
                if (meld.Type == MeldType.Pon && meld.Tiles.Count > 0 && handTypeIds.Contains(meld.Tiles[0].TileTypeId))
                {
                    result |= GameActionType.KaKan;
                    break;
                }
            }
        }

        // 九種九牌
        if (IsFirstUninterruptedDraw(state))
        {
            var terminalHonorTypes = new HashSet<int>();
            foreach (var tile in hand)
            {
                var id = tile.TileTypeId;
                if (IsTerminalOrHonor(id))
                {
                    terminalHonorTypes.Add(id);
                }
            }

            if (terminalHonorTypes.Count >= 9)
            {
                result |= GameActionType.KyuushuKyuuhai;
            }
        }

        // 北抜き（三麻のみ: 手牌に北牌があれば抜ける）
        if (state.PlayerCount == 3)
        {
            const int northTileTypeId = 30;
            for (var i = 0; i < hand.Count; i++)
            {
                if (hand[i].TileTypeId == northTileTypeId)
                {
                    result |= GameActionType.Nuki;
                    break;
                }
            }
        }

        return result;
    }

    /// <summary>
    /// 他家打牌後の合法アクションを取得する
    /// </summary>
    /// <param name="state">現在の試合状態</param>
    /// <param name="respondingPlayerId">応答するプレイヤーID</param>
    /// <param name="discardPlayerId">打牌したプレイヤーID</param>
    /// <param name="discardedTile">捨てられた牌</param>
    /// <returns>合法アクションのフラグ合成</returns>
    public static GameActionType GetValidActionsOnDiscard(
        GameState state, int respondingPlayerId, int discardPlayerId, Tile discardedTile)
    {
        var player = state.GetPlayer(respondingPlayerId);
        var hand = player.Hand;
        var meldCount = player.Melds.Count;
        var result = GameActionType.Skip;

        var counts = ToCountArray(hand);
        var discardTypeId = discardedTile.TileTypeId;

        // ロン判定
        counts[discardTypeId]++;
        if (AgariChecker.IsAgariFromCounts(counts, meldCount) && !FuritenChecker.IsFuriten(player, meldCount))
        {
            result |= GameActionType.Ron;
        }

        counts[discardTypeId]--;

        // ポン判定
        if (counts[discardTypeId] >= 2)
        {
            result |= GameActionType.Pon;
        }

        // 大明槓判定
        if (counts[discardTypeId] >= 3)
        {
            result |= GameActionType.DaiMinKan;
        }

        // チー判定
        if (state.PlayerCount != 3
            && respondingPlayerId == (discardPlayerId + 1) % state.PlayerCount
            && discardTypeId < 27)
        {
            var suitBase = (discardTypeId / 9) * 9;
            var suitMax = suitBase + 8;
            var t = discardTypeId;

            // 下位順子: t-2, t-1
            if (t - 2 >= suitBase && counts[t - 2] > 0 && counts[t - 1] > 0)
            {
                result |= GameActionType.Chi;
            }
            // 中位順子: t-1, t+1
            else if (t - 1 >= suitBase && t + 1 <= suitMax && counts[t - 1] > 0 && counts[t + 1] > 0)
            {
                result |= GameActionType.Chi;
            }
            // 上位順子: t+1, t+2
            else if (t + 2 <= suitMax && counts[t + 1] > 0 && counts[t + 2] > 0)
            {
                result |= GameActionType.Chi;
            }
        }

        return result;
    }

    /// <summary>
    /// 門前（閉じた手）かどうかを判定する
    /// </summary>
    private static bool IsClosedHand(PlayerState player)
    {
        for (var i = 0; i < player.Melds.Count; i++)
        {
            var type = player.Melds[i].Type;
            if (type is MeldType.Chi or MeldType.Pon or MeldType.DaiMinKan or MeldType.KaKan)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// 第一ツモかつ鳴きが入っていない状態かを判定する（九種九牌の条件）
    /// </summary>
    private static bool IsFirstUninterruptedDraw(GameState state)
    {
        for (var i = 0; i < state.PlayerCount; i++)
        {
            var p = state.GetPlayer(i);
            if (p.Discards.Count > 0 || p.Melds.Count > 0)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// 么九牌（端牌または字牌）かどうかを判定する
    /// </summary>
    private static bool IsTerminalOrHonor(int tileTypeId)
    {
        return tileTypeId >= 27 || tileTypeId % 9 == 0 || tileTypeId % 9 == 8;
    }

    /// <summary>
    /// 手牌をTileTypeId別のカウント配列に変換する
    /// </summary>
    private static int[] ToCountArray(IReadOnlyList<Tile> tiles)
    {
        var counts = new int[34];
        for (var i = 0; i < tiles.Count; i++)
        {
            counts[tiles[i].TileTypeId]++;
        }

        return counts;
    }
}
