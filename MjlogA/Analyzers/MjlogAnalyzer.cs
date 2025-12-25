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
/// 牌譜データの統合分析を行うクラス
/// </summary>
public class MjlogAnalyzer
{
    private readonly ScoreAnalyzer _scoreAnalyzer = new();
    private readonly RoundCountAnalyzer _roundCountAnalyzer = new();
    private readonly TurnCountAnalyzer _turnCountAnalyzer = new();
    private readonly YakuAnalyzer _yakuAnalyzer = new();
    private readonly DoraAnalyzer _doraAnalyzer = new();
    private readonly PointDistributionAnalyzer _pointDistributionAnalyzer = new();
    private int _gameCount;
    private int _roundCount;

    /// <summary>
    /// 試合データを分析に追加
    /// </summary>
    /// <param name="game">試合データ</param>
    public void Analyze(GameRecord game)
    {
        _gameCount++;
        _roundCount += game.Rounds.Count;

        _scoreAnalyzer.Analyze(game);
        _roundCountAnalyzer.Analyze(game);
        _turnCountAnalyzer.Analyze(game);
        _yakuAnalyzer.Analyze(game);
        _doraAnalyzer.Analyze(game);
        _pointDistributionAnalyzer.Analyze(game);
    }

    /// <summary>
    /// 分析結果を生成
    /// </summary>
    /// <returns>分析結果</returns>
    public AnalysisResult GetResult()
    {
        return new AnalysisResult
        {
            GameCount = _gameCount,
            RoundCount = _roundCount,
            AgariCount = _yakuAnalyzer.AgariCount,
            ScoreDistribution = DistributionStatistics.Calculate(_scoreAnalyzer.Scores),
            RoundCountDistribution = DistributionStatistics.Calculate(_roundCountAnalyzer.RoundCounts),
            TurnDistribution = DistributionStatistics.Calculate(_turnCountAnalyzer.TurnCounts),
            YakuFrequencies = _yakuAnalyzer.GetResults(),
            DoraIndicatorFrequencies = _doraAnalyzer.GetIndicatorResults(),
            ActualDoraFrequencies = _doraAnalyzer.GetActualDoraResults(),
            PointDistributionFrequencies = _pointDistributionAnalyzer.GetResults()
        };
    }
}