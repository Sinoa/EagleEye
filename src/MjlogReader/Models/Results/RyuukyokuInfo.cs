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
/// 流局の種類
/// </summary>
public enum RyuukyokuType
{
    /// <summary>通常流局（荒牌平局）</summary>
    Exhaustive,

    /// <summary>九種九牌</summary>
    NineTerminals,

    /// <summary>四風連打</summary>
    FourWinds,

    /// <summary>四槓散了</summary>
    FourKans,

    /// <summary>四家立直</summary>
    FourReach,

    /// <summary>三家和了</summary>
    TripleRon,

    /// <summary>流し満貫</summary>
    NagashiMangan
}

/// <summary>
/// 流局情報
/// </summary>
public class RyuukyokuInfo
{
    /// <summary>流局の種類</summary>
    public RyuukyokuType Type { get; set; }

    /// <summary>テンパイしているプレイヤーIDリスト</summary>
    public List<int> TenpaiPlayerIds { get; set; } = [];

    /// <summary>点数移動（プレイヤーID -> 移動点）</summary>
    public Dictionary<int, int> ScoreChanges { get; set; } = [];

    /// <summary>流し満貫達成プレイヤーIDリスト</summary>
    public List<int> NagashiManganPlayerIds { get; set; } = [];

    /// <summary>
    /// 文字列表現を取得
    /// </summary>
    public override string ToString()
    {
        var typeName = Type switch
        {
            RyuukyokuType.Exhaustive => "荒牌平局",
            RyuukyokuType.NineTerminals => "九種九牌",
            RyuukyokuType.FourWinds => "四風連打",
            RyuukyokuType.FourKans => "四槓散了",
            RyuukyokuType.FourReach => "四家立直",
            RyuukyokuType.TripleRon => "三家和了",
            RyuukyokuType.NagashiMangan => "流し満貫",
            _ => "流局"
        };

        if (TenpaiPlayerIds.Count > 0)
        {
            var tenpaiStr = string.Join(",", TenpaiPlayerIds.Select(p => $"P{p}"));
            return $"{typeName} (テンパイ: {tenpaiStr})";
        }

        return typeName;
    }
}