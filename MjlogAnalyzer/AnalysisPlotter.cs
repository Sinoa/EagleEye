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
using ScottPlot;

namespace MjlogAnalyzer;

/// <summary>
/// 分析結果をグラフとしてプロットするクラス
/// </summary>
public static class AnalysisPlotter
{
    /// <summary>
    /// 日本語表示用フォント名
    /// </summary>
    private static readonly string JapaneseFontName = GetJapaneseFontName();

    /// <summary>
    /// 日本語フォント名を取得（環境に応じて適切なフォントを選択）
    /// </summary>
    private static string GetJapaneseFontName()
    {
        // Windows: Yu Gothic UI, Meiryo UI など
        if (OperatingSystem.IsWindows())
        {
            return "Yu Gothic UI";
        }

        // macOS: Hiragino Sans など
        if (OperatingSystem.IsMacOS())
        {
            return "Hiragino Sans";
        }

        // Linux等: Noto Sans CJK JP など
        return "Noto Sans CJK JP";
    }

    /// <summary>
    /// プロットに日本語フォントを適用
    /// </summary>
    private static void ApplyJapaneseFont(Plot plot)
    {
        plot.Axes.Title.Label.FontName = JapaneseFontName;
        plot.Axes.Bottom.Label.FontName = JapaneseFontName;
        plot.Axes.Left.Label.FontName = JapaneseFontName;
        plot.Axes.Bottom.TickLabelStyle.FontName = JapaneseFontName;
        plot.Axes.Left.TickLabelStyle.FontName = JapaneseFontName;
    }
    /// <summary>
    /// 分析結果から各種グラフを生成して保存
    /// </summary>
    /// <param name="result">分析結果</param>
    /// <param name="outputDirectory">出力先ディレクトリ</param>
    /// <param name="width">グラフの幅</param>
    /// <param name="height">グラフの高さ</param>
    /// <returns>生成されたファイルのリスト</returns>
    public static List<string> SavePlots(AnalysisResult result, string outputDirectory, int width = 800, int height = 600)
    {
        Directory.CreateDirectory(outputDirectory);
        var enabled = result.EnabledAnalyzers;
        var generatedFiles = new List<string>();

        if (enabled.HasFlag(AnalyzerTypes.Score) && result.ScoreDistribution.Count > 0)
        {
            var path = SaveScoreDistributionPlot(result, outputDirectory, width, height);
            if (path != null) generatedFiles.Add(path);
        }

        if (enabled.HasFlag(AnalyzerTypes.RoundCount) && result.RoundCountDistribution.Count > 0)
        {
            var path = SaveRoundCountDistributionPlot(result, outputDirectory, width, height);
            if (path != null) generatedFiles.Add(path);
        }

        if (enabled.HasFlag(AnalyzerTypes.TurnCount) && result.TurnDistribution.Count > 0)
        {
            var path = SaveTurnDistributionPlot(result, outputDirectory, width, height);
            if (path != null) generatedFiles.Add(path);
        }

        if (enabled.HasFlag(AnalyzerTypes.Yaku) && result.YakuFrequencies.Count > 0)
        {
            var path = SaveYakuFrequencyPlot(result, outputDirectory, width, height);
            if (path != null) generatedFiles.Add(path);
        }

        if (enabled.HasFlag(AnalyzerTypes.Dora) && result.ActualDoraFrequencies.Count > 0)
        {
            var path = SaveDoraFrequencyPlot(result, outputDirectory, width, height);
            if (path != null) generatedFiles.Add(path);
        }

        if (enabled.HasFlag(AnalyzerTypes.PointDistribution) && result.PointDistributionFrequencies.Count > 0)
        {
            var path = SavePointDistributionPlot(result, outputDirectory, width, height);
            if (path != null) generatedFiles.Add(path);
        }

        return generatedFiles;
    }

    private static string? SaveScoreDistributionPlot(AnalysisResult result, string outputDir, int width, int height)
    {
        var stats = result.ScoreDistribution;
        var plot = new Plot();
        ApplyJapaneseFont(plot);

        // 箱ひげ図風の統計情報表示
        double[] positions = [1, 2, 3, 4, 5, 6];
        double[] values = [stats.Min, stats.Q1, stats.Median, stats.Q3, stats.Max, stats.Average];
        string[] labels = ["最小値", "Q1", "中央値", "Q3", "最大値", "平均"];

        var bars = plot.Add.Bars(positions, values);
        bars.Color = ScottPlot.Color.FromHex("#4a90d9");

        plot.Axes.Bottom.TickGenerator = new ScottPlot.TickGenerators.NumericManual(
            positions.Select((p, i) => new Tick(p, labels[i])).ToArray());

        plot.Title("和了時の点数分布 統計");
        plot.YLabel("点数");

        var filePath = Path.Combine(outputDir, "score_distribution.png");
        plot.SavePng(filePath, width, height);
        return filePath;
    }

    private static string? SaveRoundCountDistributionPlot(AnalysisResult result, string outputDir, int width, int height)
    {
        var stats = result.RoundCountDistribution;
        var plot = new Plot();
        ApplyJapaneseFont(plot);

        double[] positions = [1, 2, 3, 4, 5, 6];
        double[] values = [stats.Min, stats.Q1, stats.Median, stats.Q3, stats.Max, stats.Average];
        string[] labels = ["最小値", "Q1", "中央値", "Q3", "最大値", "平均"];

        var bars = plot.Add.Bars(positions, values);
        bars.Color = ScottPlot.Color.FromHex("#5cb85c");

        plot.Axes.Bottom.TickGenerator = new ScottPlot.TickGenerators.NumericManual(
            positions.Select((p, i) => new Tick(p, labels[i])).ToArray());

        plot.Title("局数分布 統計");
        plot.YLabel("局数");

        var filePath = Path.Combine(outputDir, "round_count_distribution.png");
        plot.SavePng(filePath, width, height);
        return filePath;
    }

    private static string? SaveTurnDistributionPlot(AnalysisResult result, string outputDir, int width, int height)
    {
        var stats = result.TurnDistribution;
        var plot = new Plot();
        ApplyJapaneseFont(plot);

        double[] positions = [1, 2, 3, 4, 5, 6];
        double[] values = [stats.Min, stats.Q1, stats.Median, stats.Q3, stats.Max, stats.Average];
        string[] labels = ["最小値", "Q1", "中央値", "Q3", "最大値", "平均"];

        var bars = plot.Add.Bars(positions, values);
        bars.Color = ScottPlot.Color.FromHex("#f0ad4e");

        plot.Axes.Bottom.TickGenerator = new ScottPlot.TickGenerators.NumericManual(
            positions.Select((p, i) => new Tick(p, labels[i])).ToArray());

        plot.Title("巡目分布 統計");
        plot.YLabel("巡目");

        var filePath = Path.Combine(outputDir, "turn_distribution.png");
        plot.SavePng(filePath, width, height);
        return filePath;
    }

    private static string? SaveYakuFrequencyPlot(AnalysisResult result, string outputDir, int width, int height)
    {
        var topYakus = result.YakuFrequencies.Take(20).Reverse().ToList();
        if (topYakus.Count == 0) return null;

        var plot = new Plot();
        ApplyJapaneseFont(plot);

        double[] positions = Enumerable.Range(0, topYakus.Count).Select(i => (double)i).ToArray();
        double[] counts = topYakus.Select(y => (double)y.Count).ToArray();
        string[] names = topYakus.Select(y => y.Name).ToArray();

        var bars = plot.Add.Bars(positions, counts);
        bars.Horizontal = true;
        bars.Color = ScottPlot.Color.FromHex("#d9534f");

        plot.Axes.Left.TickGenerator = new ScottPlot.TickGenerators.NumericManual(
            positions.Select((p, i) => new Tick(p, names[i])).ToArray());

        plot.Title("役の出現頻度（上位20）");
        plot.XLabel("出現回数");

        // 横向きバーのため高さを調整
        var adjustedHeight = Math.Max(height, topYakus.Count * 30 + 100);
        var filePath = Path.Combine(outputDir, "yaku_frequency.png");
        plot.SavePng(filePath, width, adjustedHeight);
        return filePath;
    }

    private static string? SaveDoraFrequencyPlot(AnalysisResult result, string outputDir, int width, int height)
    {
        var doras = result.ActualDoraFrequencies.ToList();
        if (doras.Count == 0) return null;

        var plot = new Plot();
        ApplyJapaneseFont(plot);

        double[] positions = Enumerable.Range(0, doras.Count).Select(i => (double)i).ToArray();
        double[] counts = doras.Select(d => (double)d.Count).ToArray();
        string[] names = doras.Select(d => d.Name).ToArray();

        var bars = plot.Add.Bars(positions, counts);
        bars.Color = ScottPlot.Color.FromHex("#5bc0de");

        // ラベルが多い場合は間引く
        var tickInterval = Math.Max(1, doras.Count / 20);
        var ticks = positions
            .Where((_, i) => i % tickInterval == 0)
            .Select((p, i) => new Tick(p, names[(int)p]))
            .ToArray();

        plot.Axes.Bottom.TickGenerator = new ScottPlot.TickGenerators.NumericManual(ticks);
        plot.Axes.Bottom.TickLabelStyle.Rotation = 45;

        plot.Title("ドラ牌の出現頻度");
        plot.YLabel("出現回数");

        var filePath = Path.Combine(outputDir, "dora_frequency.png");
        plot.SavePng(filePath, width, height);
        return filePath;
    }

    private static string? SavePointDistributionPlot(AnalysisResult result, string outputDir, int width, int height)
    {
        var points = result.PointDistributionFrequencies.ToList();
        if (points.Count == 0) return null;

        var plot = new Plot();
        ApplyJapaneseFont(plot);

        double[] xValues = points.Select(p => double.Parse(p.Name)).ToArray();
        double[] yValues = points.Select(p => (double)p.Count).ToArray();

        var scatter = plot.Add.ScatterLine(xValues, yValues);
        scatter.Color = ScottPlot.Color.FromHex("#9b59b6");
        scatter.LineWidth = 2;

        plot.Title("局終了時の持ち点分布");
        plot.XLabel("持ち点");
        plot.YLabel("出現回数");

        var filePath = Path.Combine(outputDir, "point_distribution.png");
        plot.SavePng(filePath, width, height);
        return filePath;
    }
}