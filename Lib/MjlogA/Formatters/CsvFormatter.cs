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

using MjlogA.Analyzers;
using MjlogA.Models;

namespace MjlogA.Formatters;

/// <summary>
/// 分析結果をCSV形式で出力するフォーマッタ
/// </summary>
public static class CsvFormatter
{
    /// <summary>
    /// 分析結果をCSV形式で出力
    /// </summary>
    /// <param name="result">分析結果</param>
    /// <param name="writer">出力先</param>
    public static void Write(AnalysisResult result, TextWriter writer)
    {
        var enabled = result.EnabledAnalyzers;

        WriteSummary(result, writer);

        if (enabled.HasFlag(AnalyzerTypes.Score))
        {
            writer.WriteLine();
            WriteScoreDistribution(result.ScoreDistribution, writer);
        }

        if (enabled.HasFlag(AnalyzerTypes.RoundCount))
        {
            writer.WriteLine();
            WriteRoundCountDistribution(result.RoundCountDistribution, writer);
        }

        if (enabled.HasFlag(AnalyzerTypes.TurnCount))
        {
            writer.WriteLine();
            WriteTurnDistribution(result.TurnDistribution, writer);
        }

        if (enabled.HasFlag(AnalyzerTypes.Yaku))
        {
            writer.WriteLine();
            WriteYakuFrequencies(result.YakuFrequencies, writer);
        }

        if (enabled.HasFlag(AnalyzerTypes.Dora))
        {
            writer.WriteLine();
            WriteDoraIndicatorFrequencies(result.DoraIndicatorFrequencies, writer);
            writer.WriteLine();
            WriteActualDoraFrequencies(result.ActualDoraFrequencies, writer);
        }

        if (enabled.HasFlag(AnalyzerTypes.PointDistribution))
        {
            writer.WriteLine();
            WritePointDistributionFrequencies(result.PointDistributionFrequencies, writer);
        }
    }

    private static void WriteSummary(AnalysisResult result, TextWriter writer)
    {
        writer.WriteLine("# サマリー");
        writer.WriteLine("項目,値");
        writer.WriteLine($"試合数,{result.GameCount}");
        writer.WriteLine($"局数,{result.RoundCount}");
        writer.WriteLine($"和了回数,{result.AgariCount}");
    }

    private static void WriteScoreDistribution(DistributionStatistics stats, TextWriter writer)
    {
        writer.WriteLine("# 和了時の点数分布");
        WriteDistributionStatistics(stats, writer);
    }

    private static void WriteRoundCountDistribution(DistributionStatistics stats, TextWriter writer)
    {
        writer.WriteLine("# 全試合の局数分布");
        WriteDistributionStatistics(stats, writer);
    }

    private static void WriteTurnDistribution(DistributionStatistics stats, TextWriter writer)
    {
        writer.WriteLine("# 全試合の巡目分布");
        WriteDistributionStatistics(stats, writer);
    }

    private static void WriteDistributionStatistics(DistributionStatistics stats, TextWriter writer)
    {
        writer.WriteLine("統計項目,値");
        writer.WriteLine($"データ数,{stats.Count}");
        writer.WriteLine($"最小値,{stats.Min:F2}");
        writer.WriteLine($"最大値,{stats.Max:F2}");
        writer.WriteLine($"平均値,{stats.Average:F2}");
        writer.WriteLine($"中央値,{stats.Median:F2}");
        writer.WriteLine($"最頻値,{stats.Mode:F2}");
        writer.WriteLine($"標準偏差,{stats.StandardDeviation:F2}");
        writer.WriteLine($"第1四分位数(Q1),{stats.Q1:F2}");
        writer.WriteLine($"第3四分位数(Q3),{stats.Q3:F2}");
        writer.WriteLine($"四分位範囲(IQR),{stats.Iqr:F2}");
    }

    private static void WriteYakuFrequencies(IReadOnlyList<FrequencyResult> frequencies, TextWriter writer)
    {
        writer.WriteLine("# 各役の出現頻度");
        writer.WriteLine("役名,出現回数,出現率");
        foreach (var freq in frequencies)
        {
            writer.WriteLine($"{freq.Name},{freq.Count},{freq.Rate:P2}");
        }
    }

    private static void WriteDoraIndicatorFrequencies(IReadOnlyList<FrequencyResult> frequencies, TextWriter writer)
    {
        writer.WriteLine("# ドラ表示牌の出現頻度");
        writer.WriteLine("牌,出現回数,出現率");
        foreach (var freq in frequencies)
        {
            writer.WriteLine($"{freq.Name},{freq.Count},{freq.Rate:P2}");
        }
    }

    private static void WriteActualDoraFrequencies(IReadOnlyList<FrequencyResult> frequencies, TextWriter writer)
    {
        writer.WriteLine("# 実際のドラ牌の出現頻度");
        writer.WriteLine("牌,出現回数,出現率");
        foreach (var freq in frequencies)
        {
            writer.WriteLine($"{freq.Name},{freq.Count},{freq.Rate:P2}");
        }
    }

    private static void WritePointDistributionFrequencies(IReadOnlyList<FrequencyResult> frequencies, TextWriter writer)
    {
        writer.WriteLine("# 局終了時の持ち点分布（100点単位）");
        writer.WriteLine("持ち点,出現回数,出現率");
        foreach (var freq in frequencies)
        {
            writer.WriteLine($"{freq.Name},{freq.Count},{freq.Rate:P2}");
        }
    }
}