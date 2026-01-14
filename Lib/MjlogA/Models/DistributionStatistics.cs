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

namespace MjlogA.Models;

/// <summary>
/// 分布統計を表すレコード
/// </summary>
/// <param name="Count">データ数</param>
/// <param name="Min">最小値</param>
/// <param name="Max">最大値</param>
/// <param name="Average">平均値</param>
/// <param name="Median">中央値（第2四分位数）</param>
/// <param name="Mode">最頻値</param>
/// <param name="StandardDeviation">標準偏差</param>
/// <param name="Q1">第1四分位数（25パーセンタイル）</param>
/// <param name="Q3">第3四分位数（75パーセンタイル）</param>
/// <param name="Iqr">四分位範囲（Q3 - Q1）</param>
public record DistributionStatistics(
    int Count,
    double Min,
    double Max,
    double Average,
    double Median,
    double Mode,
    double StandardDeviation,
    double Q1,
    double Q3,
    double Iqr)
{
    /// <summary>
    /// 空の統計情報
    /// </summary>
    public static DistributionStatistics Empty => new(0, 0, 0, 0, 0, 0, 0, 0, 0, 0);

    /// <summary>
    /// 数値リストから統計情報を計算
    /// </summary>
    /// <param name="values">数値リスト</param>
    /// <returns>統計情報</returns>
    public static DistributionStatistics Calculate(IReadOnlyList<double> values)
    {
        if (values.Count == 0)
        {
            return Empty;
        }

        var sorted = values.OrderBy(v => v).ToList();
        var count = sorted.Count;
        var min = sorted[0];
        var max = sorted[^1];
        var average = sorted.Average();

        // 中央値（第2四分位数）
        var median = CalculatePercentile(sorted, 0.5);

        // 第1四分位数（25パーセンタイル）
        var q1 = CalculatePercentile(sorted, 0.25);

        // 第3四分位数（75パーセンタイル）
        var q3 = CalculatePercentile(sorted, 0.75);

        // 四分位範囲
        var iqr = q3 - q1;

        // 最頻値
        var mode = sorted
            .GroupBy(v => v)
            .OrderByDescending(g => g.Count())
            .ThenBy(g => g.Key)
            .First()
            .Key;

        // 標準偏差
        var variance = sorted.Sum(v => Math.Pow(v - average, 2)) / count;
        var standardDeviation = Math.Sqrt(variance);

        return new DistributionStatistics(count, min, max, average, median, mode, standardDeviation, q1, q3, iqr);
    }

    /// <summary>
    /// パーセンタイルを計算（線形補間）
    /// </summary>
    /// <param name="sorted">ソート済みリスト</param>
    /// <param name="percentile">パーセンタイル（0.0〜1.0）</param>
    /// <returns>パーセンタイル値</returns>
    private static double CalculatePercentile(List<double> sorted, double percentile)
    {
        if (sorted.Count == 1)
        {
            return sorted[0];
        }

        var index = percentile * (sorted.Count - 1);
        var lower = (int)Math.Floor(index);
        var upper = (int)Math.Ceiling(index);

        if (lower == upper)
        {
            return sorted[lower];
        }

        var fraction = index - lower;
        return sorted[lower] + (sorted[upper] - sorted[lower]) * fraction;
    }

    /// <summary>
    /// 整数リストから統計情報を計算
    /// </summary>
    /// <param name="values">整数リスト</param>
    /// <returns>統計情報</returns>
    public static DistributionStatistics Calculate(IReadOnlyList<int> values)
    {
        return Calculate(values.Select(v => (double)v).ToList());
    }
}