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

using Foxtamp.MjlogReplayer.Models;

namespace Foxtamp.MjlogReplayer.Rules;

/// <summary>
/// 振聴判定を行う静的クラス（Phase 0: 永続振聴のみ）
/// </summary>
public static class FuritenChecker
{
    /// <summary>
    /// 永続振聴かどうかを判定する（自分の捨て牌に待ち牌が含まれているか）
    /// </summary>
    /// <param name="player">プレイヤー状態</param>
    /// <param name="meldCount">副露数</param>
    /// <returns>振聴の場合true</returns>
    public static bool IsFuriten(PlayerState player, int meldCount)
    {
        var waits = TenpaiChecker.GetWaitingTileTypeIds(player.Hand, meldCount);
        if (waits.Count == 0)
        {
            return false;
        }

        var waitSet = new HashSet<int>(waits);
        for (var i = 0; i < player.Discards.Count; i++)
        {
            if (waitSet.Contains(player.Discards[i].Tile.TileTypeId))
            {
                return true;
            }
        }

        return false;
    }
}