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

namespace Foxtamp.MjlogReader.Models.Results;

/// <summary>
/// 和了情報
/// </summary>
public class AgariInfo
{
    /// <summary>和了したプレイヤーID（0-3）</summary>
    public int WinnerId { get; set; }

    /// <summary>放銃したプレイヤーID（0-3、ツモの場合は和了者と同じ）</summary>
    public int LoserId { get; set; }

    /// <summary>ツモ和了かどうか</summary>
    public bool IsTsumo => WinnerId == LoserId;

    /// <summary>和了牌</summary>
    public Tile? WinningTile { get; set; }

    /// <summary>手牌</summary>
    public List<Tile> Hand { get; set; } = [];

    /// <summary>副露（鳴き）</summary>
    public List<MeldInfo> Melds { get; set; } = [];

    /// <summary>ドラ表示牌</summary>
    public List<Tile> DoraIndicators { get; set; } = [];

    /// <summary>裏ドラ表示牌</summary>
    public List<Tile> UraDoraIndicators { get; set; } = [];

    /// <summary>得点</summary>
    public int Score { get; set; }

    /// <summary>符</summary>
    public int Fu { get; set; }

    /// <summary>飜数</summary>
    public int Han { get; set; }

    /// <summary>役満倍数（1=役満, 2=ダブル役満...、役満でない場合は0）</summary>
    public int Yakuman { get; set; }

    /// <summary>成立した役のリスト</summary>
    public List<YakuInfo> Yakus { get; set; } = [];

    /// <summary>点数移動（プレイヤーID -> 移動点）</summary>
    public Dictionary<int, int> ScoreChanges { get; set; } = [];

    /// <summary>
    /// 文字列表現を取得
    /// </summary>
    public override string ToString()
    {
        var type = IsTsumo ? "ツモ" : "ロン";
        var yakuStr = string.Join(", ", Yakus.Select(y => y.Name));
        return $"P{WinnerId} {type} {Score}点 [{yakuStr}]";
    }
}