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

using Foxtamp.MjlogReader.Models.Results;

namespace Foxtamp.MjlogReader.Models;

/// <summary>
/// 牌譜セッション（INIT から AGARI または RYUUKYOKU までの1局分）
/// </summary>
public class MjlogSession
{
    /// <summary>プレイヤー人数（3または4）</summary>
    public int PlayerCount { get; }

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
    public int[] StartScores { get; set; }

    /// <summary>各プレイヤーの配牌</summary>
    public List<Tile>[] InitialHands { get; set; }

    /// <summary>ドラ表示牌（初期）</summary>
    public Tile? InitialDoraIndicator { get; set; }

    /// <summary>全ドラ表示牌（追加ドラ含む）</summary>
    public List<Tile> DoraIndicators { get; set; } = [];

    /// <summary>裏ドラ表示牌</summary>
    public List<Tile> UraDoraIndicators { get; set; } = [];

    /// <summary>行動ステップ履歴</summary>
    public List<MjlogStep> Steps { get; set; } = [];

    /// <summary>セッションの結果（和了または流局）</summary>
    public MjlogSessionResult? Result { get; set; }

    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="playerCount">プレイヤー人数（3または4）</param>
    public MjlogSession(int playerCount)
    {
        PlayerCount = playerCount;
        StartScores = new int[playerCount];
        InitialHands = new List<Tile>[playerCount];
        for (var i = 0; i < playerCount; i++)
        {
            InitialHands[i] = [];
        }
    }
}