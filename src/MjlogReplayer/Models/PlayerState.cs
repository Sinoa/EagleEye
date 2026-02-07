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

namespace Foxtamp.MjlogReplayer.Models;

/// <summary>
/// プレイヤーの状態を表すイミュータブルなレコード
/// </summary>
/// <param name="PlayerId">プレイヤーID（0-3）</param>
/// <param name="Hand">手牌</param>
/// <param name="Discards">捨て牌</param>
/// <param name="Melds">副露（鳴き）</param>
/// <param name="IsReach">リーチ状態かどうか</param>
/// <param name="ReachTurnNumber">リーチ宣言時の巡目（未リーチの場合はnull）</param>
/// <param name="Score">現在の得点</param>
public record PlayerState(
    int PlayerId,
    IReadOnlyList<Tile> Hand,
    IReadOnlyList<DiscardedTile> Discards,
    IReadOnlyList<MeldInfo> Melds,
    bool IsReach,
    int? ReachTurnNumber,
    int Score)
{
    /// <summary>
    /// 初期状態のプレイヤー状態を作成
    /// </summary>
    /// <param name="playerId">プレイヤーID</param>
    /// <param name="initialHand">配牌</param>
    /// <param name="initialScore">初期得点</param>
    /// <returns>初期状態のプレイヤー状態</returns>
    public static PlayerState CreateInitial(int playerId, IReadOnlyList<Tile> initialHand, int initialScore)
    {
        return new PlayerState(
            playerId,
            initialHand,
            [],
            [],
            false,
            null,
            initialScore);
    }

    /// <summary>
    /// 手牌に牌を追加（ツモ）
    /// </summary>
    /// <param name="tile">ツモった牌</param>
    /// <returns>更新されたプレイヤー状態</returns>
    public PlayerState AddTileToHand(Tile tile)
    {
        var newHand = Hand.Append(tile).ToList();
        return this with { Hand = newHand };
    }

    /// <summary>
    /// 手牌から牌を削除して捨て牌に追加（打牌）
    /// </summary>
    /// <param name="tile">捨てる牌</param>
    /// <param name="isTsumogiri">ツモ切りかどうか</param>
    /// <param name="isReachDeclare">リーチ宣言牌かどうか</param>
    /// <returns>更新されたプレイヤー状態</returns>
    public PlayerState DiscardTile(Tile tile, bool isTsumogiri, bool isReachDeclare)
    {
        var newHand = RemoveTileFromHand(Hand, tile);
        var discardedTile = new DiscardedTile(tile, isTsumogiri, isReachDeclare, null);
        var newDiscards = Discards.Append(discardedTile).ToList();

        return this with { Hand = newHand, Discards = newDiscards };
    }

    /// <summary>
    /// リーチ状態に変更
    /// </summary>
    /// <param name="turnNumber">リーチ宣言時の巡目</param>
    /// <returns>更新されたプレイヤー状態</returns>
    public PlayerState DeclareReach(int turnNumber)
    {
        return this with { IsReach = true, ReachTurnNumber = turnNumber };
    }

    /// <summary>
    /// 得点を変更
    /// </summary>
    /// <param name="newScore">新しい得点</param>
    /// <returns>更新されたプレイヤー状態</returns>
    public PlayerState WithScore(int newScore)
    {
        return this with { Score = newScore };
    }

    /// <summary>
    /// 副露を追加し手牌から対応する牌を削除
    /// </summary>
    /// <param name="meld">副露情報</param>
    /// <returns>更新されたプレイヤー状態</returns>
    public PlayerState AddMeld(MeldInfo meld)
    {
        var newMelds = Melds.Append(meld).ToList();

        // 副露で使用した牌を手牌から削除（CalledTile以外）
        var newHand = Hand.ToList();
        foreach (var tile in meld.Tiles)
        {
            // CalledTileは他家から取得した牌なので手牌から削除しない
            if (meld.CalledTile != null && tile.OriginalId == meld.CalledTile.OriginalId)
            {
                continue;
            }

            newHand = RemoveTileFromHand(newHand, tile);
        }

        return this with { Hand = newHand, Melds = newMelds };
    }

    /// <summary>
    /// 加槓を処理（既存のポンに牌を追加）
    /// </summary>
    /// <param name="meld">加槓情報</param>
    /// <returns>更新されたプレイヤー状態</returns>
    public PlayerState AddKaKan(MeldInfo meld)
    {
        // 対応するポンを探して加槓に更新
        var newMelds = Melds.ToList();
        for (var i = 0; i < newMelds.Count; i++)
        {
            if (newMelds[i].Type == MeldType.Pon
                && newMelds[i].Tiles.Count > 0
                && meld.Tiles.Count > 0
                && newMelds[i].Tiles[0].TileTypeId == meld.Tiles[0].TileTypeId)
            {
                newMelds[i] = meld;
                break;
            }
        }

        // 加槓で使用した牌を手牌から削除（ポンの3枚以外の1枚）
        var newHand = Hand.ToList();
        if (meld.CalledTile != null)
        {
            newHand = RemoveTileFromHand(newHand, meld.CalledTile);
        }

        return this with { Hand = newHand, Melds = newMelds };
    }

    /// <summary>
    /// 最後の捨て牌を鳴かれた状態にマーク
    /// </summary>
    /// <param name="calledByPlayerId">鳴いたプレイヤーID</param>
    /// <returns>更新されたプレイヤー状態</returns>
    public PlayerState MarkLastDiscardAsCalled(int calledByPlayerId)
    {
        if (Discards.Count == 0)
        {
            return this;
        }

        var newDiscards = Discards.ToList();
        var lastIndex = newDiscards.Count - 1;
        newDiscards[lastIndex] = newDiscards[lastIndex].MarkAsCalled(calledByPlayerId);

        return this with { Discards = newDiscards };
    }

    /// <summary>
    /// 手牌から指定した牌を1枚削除
    /// </summary>
    private static List<Tile> RemoveTileFromHand(IEnumerable<Tile> hand, Tile tile)
    {
        var list = hand.ToList();
        var index = list.FindIndex(t => t.OriginalId == tile.OriginalId);
        if (index >= 0)
        {
            list.RemoveAt(index);
        }

        return list;
    }

    /// <summary>
    /// 文字列表現を取得
    /// </summary>
    public override string ToString()
    {
        var handStr = string.Join("", Hand.Select(t => t.DisplayName));
        var meldStr = Melds.Count > 0 ? $" 副露:{string.Join(",", Melds)}" : "";
        var reachStr = IsReach ? $" [リーチ(巡目{ReachTurnNumber})]" : "";
        return $"P{PlayerId}: {handStr}{meldStr}{reachStr} ({Score}点)";
    }
}