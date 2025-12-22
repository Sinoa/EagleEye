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

namespace MjlogConverter.Models;

/// <summary>
/// 流局の種類
/// </summary>
public enum DrawType
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
/// 和了結果
/// </summary>
public class AgariResult
{
    /// <summary>和了したプレイヤーID</summary>
    public int WinnerId { get; set; }

    /// <summary>放銃したプレイヤーID（ツモの場合は-1）</summary>
    public int LoserId { get; set; } = -1;

    /// <summary>ツモ和了かどうか</summary>
    public bool IsTsumo { get; set; }

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

    /// <summary>役満の場合の倍数（1=役満, 2=ダブル役満...）</summary>
    public int Yakuman { get; set; }

    /// <summary>成立した役のリスト（役名, 飜数）</summary>
    public List<YakuInfo> Yakus { get; set; } = [];

    /// <summary>点数移動（プレイヤーID -> 移動点）</summary>
    public Dictionary<int, int> ScoreChanges { get; set; } = [];
}

/// <summary>
/// 役情報
/// </summary>
public class YakuInfo
{
    /// <summary>役ID（天鳳形式）</summary>
    public int Id { get; set; }

    /// <summary>役名</summary>
    public string Name { get; set; } = "";

    /// <summary>飜数（役満の場合は0）</summary>
    public int Han { get; set; }

    /// <summary>役満倍数（役満でない場合は0）</summary>
    public int Yakuman { get; set; }
}

/// <summary>
/// 流局結果
/// </summary>
public class DrawResult
{
    /// <summary>流局の種類</summary>
    public DrawType Type { get; set; }

    /// <summary>テンパイしているプレイヤーIDリスト</summary>
    public List<int> TenpaiPlayerIds { get; set; } = [];

    /// <summary>点数移動（プレイヤーID -> 移動点）</summary>
    public Dictionary<int, int> ScoreChanges { get; set; } = [];

    /// <summary>流し満貫達成プレイヤーIDリスト</summary>
    public List<int> NagashiManganPlayerIds { get; set; } = [];
}

/// <summary>
/// 局の結果
/// </summary>
public class RoundResult
{
    /// <summary>和了による終局かどうか</summary>
    public bool IsAgari { get; set; }

    /// <summary>和了結果（複数の場合はダブロン・トリロン）</summary>
    public List<AgariResult> AgariResults { get; set; } = [];

    /// <summary>流局結果</summary>
    public DrawResult? DrawResult { get; set; }

    /// <summary>終局後の各プレイヤーの得点</summary>
    public int[] FinalScores { get; set; } = new int[4];
}