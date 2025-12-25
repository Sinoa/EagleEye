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
/// ドラ牌の出現頻度を分析する
/// </summary>
public class DoraAnalyzer
{
    private readonly FrequencyCollection _indicatorCounts = new();
    private readonly FrequencyCollection _actualDoraCounts = new();
    private int _roundCount;

    /// <summary>
    /// 試合データからドラ牌の出現を収集
    /// </summary>
    /// <param name="game">試合データ</param>
    public void Analyze(GameRecord game)
    {
        foreach (var round in game.Rounds)
        {
            _roundCount++;
            foreach (var indicator in round.DoraIndicators)
            {
                // ドラ表示牌
                _indicatorCounts.Add(indicator.DisplayName);

                // 実際のドラ牌
                var actualDora = ConvertToActualDora(indicator);
                _actualDoraCounts.Add(actualDora);
            }
        }
    }

    /// <summary>
    /// ドラ表示牌から実際のドラ牌を算出
    /// </summary>
    /// <param name="indicator">ドラ表示牌</param>
    /// <returns>実際のドラ牌の表示名</returns>
    private static string ConvertToActualDora(Tile indicator)
    {
        return indicator.Suit switch
        {
            TileSuit.Man => ConvertNumberTile(indicator.Number, "m"),
            TileSuit.Pin => ConvertNumberTile(indicator.Number, "p"),
            TileSuit.Sou => ConvertNumberTile(indicator.Number, "s"),
            TileSuit.Honor => ConvertHonorTile(indicator.Number),
            _ => indicator.DisplayName
        };
    }

    /// <summary>
    /// 数牌のドラを算出（9→1でループ）
    /// </summary>
    private static string ConvertNumberTile(int number, string suit)
    {
        var nextNumber = number == 9 ? 1 : number + 1;
        return $"{nextNumber}{suit}";
    }

    /// <summary>
    /// 字牌のドラを算出
    /// 東→南→西→北→東、白→發→中→白
    /// </summary>
    private static string ConvertHonorTile(int honorNumber)
    {
        return (HonorType)honorNumber switch
        {
            HonorType.East => "南",
            HonorType.South => "西",
            HonorType.West => "北",
            HonorType.North => "東",
            HonorType.White => "發",
            HonorType.Green => "中",
            HonorType.Red => "白",
            _ => "?"
        };
    }

    /// <summary>
    /// 局数
    /// </summary>
    public int RoundCount => _roundCount;

    /// <summary>
    /// ドラ表示牌の出現頻度を取得（局数を母数とした出現率）
    /// </summary>
    public IReadOnlyList<FrequencyResult> GetIndicatorResults()
    {
        return _indicatorCounts.GetResultsWithTotal(_roundCount);
    }

    /// <summary>
    /// 実際のドラ牌の出現頻度を取得（局数を母数とした出現率）
    /// </summary>
    public IReadOnlyList<FrequencyResult> GetActualDoraResults()
    {
        return _actualDoraCounts.GetResultsWithTotal(_roundCount);
    }
}