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

namespace MjlogA.Analyzers;

/// <summary>
/// 分析結果をまとめて保持するクラス
/// </summary>
public class AnalysisResult
{
    /// <summary>処理した試合数</summary>
    public int GameCount { get; set; }

    /// <summary>処理した局数</summary>
    public int RoundCount { get; set; }

    /// <summary>和了回数</summary>
    public int AgariCount { get; set; }

    /// <summary>和了時の点数分布</summary>
    public DistributionStatistics ScoreDistribution { get; set; } = DistributionStatistics.Empty;

    /// <summary>全試合の局数分布</summary>
    public DistributionStatistics RoundCountDistribution { get; set; } = DistributionStatistics.Empty;

    /// <summary>全試合の巡目分布</summary>
    public DistributionStatistics TurnDistribution { get; set; } = DistributionStatistics.Empty;

    /// <summary>各役の出現頻度</summary>
    public IReadOnlyList<FrequencyResult> YakuFrequencies { get; set; } = [];

    /// <summary>ドラ表示牌の出現頻度</summary>
    public IReadOnlyList<FrequencyResult> DoraIndicatorFrequencies { get; set; } = [];

    /// <summary>実際のドラ牌の出現頻度</summary>
    public IReadOnlyList<FrequencyResult> ActualDoraFrequencies { get; set; } = [];
}