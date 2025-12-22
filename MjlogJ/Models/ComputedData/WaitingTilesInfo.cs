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

namespace MjlogJ.Models.ComputedData;

/// <summary>
/// 待ち牌情報（計算による追加情報）
/// </summary>
public class WaitingTilesInfo
{
    /// <summary>プレイヤーID</summary>
    public int PlayerId { get; set; }

    /// <summary>待ち牌のリスト</summary>
    public List<Tile> WaitingTiles { get; set; } = [];

    /// <summary>待ち牌の種類数</summary>
    public int WaitingKinds => WaitingTiles.Select(t => t.TileTypeId).Distinct().Count();

    /// <summary>待ち牌の残り枚数（計算時点で見えている牌を除外）</summary>
    public int RemainingCount { get; set; }

    /// <summary>計算時点のシーケンス番号</summary>
    public int AtSequence { get; set; }

    /// <summary>計算時点の手牌</summary>
    public List<Tile> HandAtCalculation { get; set; } = [];

    /// <summary>計算時点の副露</summary>
    public List<MeldInfo> MeldsAtCalculation { get; set; } = [];
}