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
    private readonly AnalyzerTypes _enabledAnalyzers;
    private readonly ScoreAnalyzer? _scoreAnalyzer;
    private readonly RoundCountAnalyzer? _roundCountAnalyzer;
    private readonly TurnCountAnalyzer? _turnCountAnalyzer;
    private readonly YakuAnalyzer? _yakuAnalyzer;
    private readonly DoraAnalyzer? _doraAnalyzer;
    private readonly PointDistributionAnalyzer? _pointDistributionAnalyzer;
    private int _gameCount;
    private int _roundCount;

    /// <summary>
    /// すべての分析器を有効にしてインスタンスを生成
    /// </summary>
    public MjlogAnalyzer() : this(AnalyzerTypes.All)
    {
    }

    /// <summary>
    /// 指定した分析器のみを有効にしてインスタンスを生成
    /// </summary>
    /// <param name="enabledAnalyzers">有効にする分析器</param>
    public MjlogAnalyzer(AnalyzerTypes enabledAnalyzers)
    {
        _enabledAnalyzers = enabledAnalyzers;

        if (enabledAnalyzers.HasFlag(AnalyzerTypes.Score))
        {
            _scoreAnalyzer = new ScoreAnalyzer();
        }

        if (enabledAnalyzers.HasFlag(AnalyzerTypes.RoundCount))
        {
            _roundCountAnalyzer = new RoundCountAnalyzer();
        }

        if (enabledAnalyzers.HasFlag(AnalyzerTypes.TurnCount))
        {
            _turnCountAnalyzer = new TurnCountAnalyzer();
        }

        if (enabledAnalyzers.HasFlag(AnalyzerTypes.Yaku))
        {
            _yakuAnalyzer = new YakuAnalyzer();
        }

        if (enabledAnalyzers.HasFlag(AnalyzerTypes.Dora))
        {
            _doraAnalyzer = new DoraAnalyzer();
        }

        if (enabledAnalyzers.HasFlag(AnalyzerTypes.PointDistribution))
        {
            _pointDistributionAnalyzer = new PointDistributionAnalyzer();
        }
    }

    /// <summary>
    /// 有効な分析器の種類を取得
    /// </summary>
    public AnalyzerTypes EnabledAnalyzers => _enabledAnalyzers;

    /// <summary>
    /// 試合データを分析に追加
    /// </summary>
    /// <param name="game">試合データ</param>
    public void Analyze(GameRecord game)
    {
        _gameCount++;
        _roundCount += game.Rounds.Count;

        _scoreAnalyzer?.Analyze(game);
        _roundCountAnalyzer?.Analyze(game);
        _turnCountAnalyzer?.Analyze(game);
        _yakuAnalyzer?.Analyze(game);
        _doraAnalyzer?.Analyze(game);
        _pointDistributionAnalyzer?.Analyze(game);
    }

    /// <summary>
    /// 分析結果を生成
    /// </summary>
    /// <returns>分析結果</returns>
    public AnalysisResult GetResult()
    {
        return new AnalysisResult
        {
            EnabledAnalyzers = _enabledAnalyzers,
            GameCount = _gameCount,
            RoundCount = _roundCount,
            AgariCount = _yakuAnalyzer?.AgariCount ?? 0,
            ScoreDistribution = _scoreAnalyzer != null
                ? DistributionStatistics.Calculate(_scoreAnalyzer.Scores)
                : DistributionStatistics.Empty,
            RoundCountDistribution = _roundCountAnalyzer != null
                ? DistributionStatistics.Calculate(_roundCountAnalyzer.RoundCounts)
                : DistributionStatistics.Empty,
            TurnDistribution = _turnCountAnalyzer != null
                ? DistributionStatistics.Calculate(_turnCountAnalyzer.TurnCounts)
                : DistributionStatistics.Empty,
            YakuFrequencies = _yakuAnalyzer?.GetResults() ?? [],
            DoraIndicatorFrequencies = _doraAnalyzer?.GetIndicatorResults() ?? [],
            ActualDoraFrequencies = _doraAnalyzer?.GetActualDoraResults() ?? [],
            PointDistributionFrequencies = _pointDistributionAnalyzer?.GetResults() ?? []
        };
    }
}