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
/// 捨て牌を表すレコード
/// </summary>
/// <param name="Tile">捨てた牌</param>
/// <param name="IsTsumogiri">ツモ切りかどうか</param>
/// <param name="IsReachDeclare">リーチ宣言牌かどうか</param>
/// <param name="CalledByPlayerId">鳴かれた場合の相手プレイヤーID（鳴かれていない場合はnull）</param>
public record DiscardedTile(
    Tile Tile,
    bool IsTsumogiri,
    bool IsReachDeclare,
    int? CalledByPlayerId)
{
    /// <summary>
    /// 鳴かれたかどうか
    /// </summary>
    public bool IsCalled => CalledByPlayerId.HasValue;

    /// <summary>
    /// 捨て牌をマークして鳴かれた状態にする
    /// </summary>
    /// <param name="calledByPlayerId">鳴いたプレイヤーID</param>
    /// <returns>鳴かれた状態の捨て牌</returns>
    public DiscardedTile MarkAsCalled(int calledByPlayerId)
    {
        return this with { CalledByPlayerId = calledByPlayerId };
    }

    /// <summary>
    /// 文字列表現を取得
    /// </summary>
    public override string ToString()
    {
        var marks = new List<string>();
        if (IsTsumogiri) marks.Add("ツモ切り");
        if (IsReachDeclare) marks.Add("リーチ");
        if (IsCalled) marks.Add($"鳴かれ(P{CalledByPlayerId})");

        var markStr = marks.Count > 0 ? $" [{string.Join(", ", marks)}]" : "";
        return $"{Tile.DisplayName}{markStr}";
    }
}