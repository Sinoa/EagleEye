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

using MjlogJ.Models;

namespace MjlogA.Analyzers;

/// <summary>
/// 巡目を分析する
/// </summary>
/// <remarks>
/// 巡目は親のツモ回数でカウントします。
/// 親が1回目のツモをしたら1巡目、2回目のツモをしたら2巡目...
/// </remarks>
public class TurnCountAnalyzer
{
    private readonly List<int> _turnCounts = [];

    /// <summary>
    /// 試合データから巡目を収集
    /// </summary>
    /// <param name="game">試合データ</param>
    public void Analyze(GameRecord game)
    {
        foreach (var round in game.Rounds)
        {
            var turn = CalculateTurn(round);
            _turnCounts.Add(turn);
        }
    }

    /// <summary>
    /// 局の終了時点での巡目を計算
    /// </summary>
    /// <param name="round">局データ</param>
    /// <returns>巡目</returns>
    private static int CalculateTurn(RoundRecord round)
    {
        var dealerId = round.DealerId;
        var dealerDrawCount = 0;

        foreach (var action in round.Actions)
        {
            if (action is DrawAction drawAction && drawAction.PlayerId == dealerId)
            {
                dealerDrawCount++;
            }
        }

        // 親の配牌後の最初のツモを1巡目とする
        // 配牌は13枚で、最初のツモで14枚目を引くので、
        // 親のツモ回数がそのまま巡目になる
        return dealerDrawCount;
    }

    /// <summary>
    /// 収集した巡目のリストを取得
    /// </summary>
    public IReadOnlyList<int> TurnCounts => _turnCounts;
}