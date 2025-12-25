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

using MjlogA.Models;
using MjlogJ.Models;

namespace MjlogA.Analyzers;

/// <summary>
/// 局終了時の持ち点分布を分析する
/// </summary>
public class PointDistributionAnalyzer
{
    private readonly FrequencyCollection _pointFrequencies = new();

    /// <summary>
    /// 試合データから局終了時の持ち点を収集
    /// </summary>
    /// <param name="game">試合データ</param>
    public void Analyze(GameRecord game)
    {
        // 3人麻雀か4人麻雀かを判定
        var playerCount = game.Rule?.IsThreePlayer == true ? 3 : 4;

        foreach (var round in game.Rounds)
        {
            if (round.Result == null)
            {
                continue;
            }

            // 各プレイヤーの終了時持ち点を100点単位で切り捨てて集計
            for (var i = 0; i < playerCount; i++)
            {
                var score = round.Result.FinalScores[i];
                var roundedScore = FloorToHundred(score);
                _pointFrequencies.Add(roundedScore.ToString());
            }
        }
    }

    /// <summary>
    /// 100点単位で切り捨て
    /// </summary>
    /// <param name="score">元の点数</param>
    /// <returns>100点単位に切り捨てた点数</returns>
    private static int FloorToHundred(int score)
    {
        // 負の場合も正しく切り捨てる（例: -150 → -200）
        if (score >= 0)
        {
            return score / 100 * 100;
        }

        return (score - 99) / 100 * 100;
    }

    /// <summary>
    /// 収集した持ち点分布の結果を昇順で取得
    /// </summary>
    /// <returns>持ち点分布の頻度結果リスト（持ち点の昇順）</returns>
    public IReadOnlyList<FrequencyResult> GetResults()
    {
        var results = _pointFrequencies.GetResults();

        // 持ち点（数値）の昇順でソート
        return results
            .OrderBy(r => int.Parse(r.Name))
            .ToList();
    }
}