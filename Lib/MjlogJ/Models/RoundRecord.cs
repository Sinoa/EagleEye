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

using MjlogJ.Models.ComputedData;

namespace MjlogJ.Models;

/// <summary>
/// 局の記録
/// </summary>
public class RoundRecord
{
    /// <summary>場風（0=東, 1=南, 2=西, 3=北）</summary>
    public int RoundWind { get; set; }

    /// <summary>局番号（0-3: 東1-4局、4-7: 南1-4局...）</summary>
    public int RoundNumber { get; set; }

    /// <summary>局の表示名（例: "東1局"）</summary>
    public string RoundName => $"{RoundWindName}{RoundNumber % 4 + 1}局";

    private string RoundWindName => (RoundNumber / 4) switch
    {
        0 => "東",
        1 => "南",
        2 => "西",
        3 => "北",
        _ => "?"
    };

    /// <summary>本場数</summary>
    public int Honba { get; set; }

    /// <summary>供託リーチ棒の数</summary>
    public int Kyotaku { get; set; }

    /// <summary>親プレイヤーID（0-3）</summary>
    public int DealerId { get; set; }

    /// <summary>各プレイヤーの開始時得点</summary>
    public int[] StartScores { get; set; } = new int[4];

    /// <summary>各プレイヤーの配牌</summary>
    public List<Tile>[] InitialHands { get; set; } = [[], [], [], []];

    /// <summary>ドラ表示牌（初期）</summary>
    public Tile? InitialDoraIndicator { get; set; }

    /// <summary>全ドラ表示牌（追加ドラ含む）</summary>
    public List<Tile> DoraIndicators { get; set; } = [];

    /// <summary>裏ドラ表示牌</summary>
    public List<Tile> UraDoraIndicators { get; set; } = [];

    /// <summary>行動履歴</summary>
    public List<PlayerAction> Actions { get; set; } = [];

    /// <summary>局の結果</summary>
    public RoundResult? Result { get; set; }

    /// <summary>計算による追加情報（待ち牌など）</summary>
    public RoundComputedData? ComputedData { get; set; }
}

/// <summary>
/// 局の計算追加情報
/// </summary>
public class RoundComputedData
{
    /// <summary>リーチ時の待ち牌情報（プレイヤーID -> 待ち牌情報）</summary>
    public Dictionary<int, WaitingTilesInfo> ReachWaitingTiles { get; set; } = [];

    /// <summary>テンパイ時の待ち牌情報（プレイヤーID -> 待ち牌情報）</summary>
    public Dictionary<int, WaitingTilesInfo> TenpaiWaitingTiles { get; set; } = [];
}