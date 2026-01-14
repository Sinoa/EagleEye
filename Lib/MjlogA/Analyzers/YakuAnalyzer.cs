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
/// 役の出現頻度を分析する
/// </summary>
public class YakuAnalyzer
{
    private readonly FrequencyCollection _yakuCounts = new();
    private int _agariCount;

    /// <summary>
    /// 試合データから役の出現を収集
    /// </summary>
    /// <param name="game">試合データ</param>
    public void Analyze(GameRecord game)
    {
        foreach (var round in game.Rounds)
        {
            if (round.Result?.IsAgari == true)
            {
                foreach (var agari in round.Result.AgariResults)
                {
                    _agariCount++;
                    foreach (var yaku in agari.Yakus)
                    {
                        _yakuCounts.Add(yaku.Name);
                    }
                }
            }
        }
    }

    /// <summary>
    /// 和了回数
    /// </summary>
    public int AgariCount => _agariCount;

    /// <summary>
    /// 役の出現頻度を取得（和了回数を母数とした出現率）
    /// </summary>
    public IReadOnlyList<FrequencyResult> GetResults()
    {
        return _yakuCounts.GetResultsWithTotal(_agariCount);
    }
}