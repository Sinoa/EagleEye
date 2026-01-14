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
/// 出現頻度を表すレコード
/// </summary>
/// <param name="Name">項目名</param>
/// <param name="Count">出現回数</param>
/// <param name="Rate">出現率（0.0〜1.0）</param>
public record FrequencyResult(string Name, int Count, double Rate);

/// <summary>
/// 出現頻度のコレクション
/// </summary>
public class FrequencyCollection
{
    private readonly Dictionary<string, int> _counts = new();
    private int _totalCount;

    /// <summary>
    /// 項目をカウントに追加
    /// </summary>
    /// <param name="name">項目名</param>
    /// <param name="count">追加するカウント数</param>
    public void Add(string name, int count = 1)
    {
        if (!_counts.TryAdd(name, count))
        {
            _counts[name] += count;
        }

        _totalCount += count;
    }

    /// <summary>
    /// 全項目の合計カウント
    /// </summary>
    public int TotalCount => _totalCount;

    /// <summary>
    /// 項目数
    /// </summary>
    public int ItemCount => _counts.Count;

    /// <summary>
    /// 結果を出現回数の降順で取得
    /// </summary>
    /// <returns>出現頻度結果のリスト</returns>
    public IReadOnlyList<FrequencyResult> GetResults()
    {
        return _counts
            .Select(kvp => new FrequencyResult(
                kvp.Key,
                kvp.Value,
                _totalCount > 0 ? (double)kvp.Value / _totalCount : 0))
            .OrderByDescending(r => r.Count)
            .ThenBy(r => r.Name)
            .ToList();
    }

    /// <summary>
    /// 指定した総数で出現率を計算した結果を取得
    /// </summary>
    /// <param name="totalForRate">出現率計算に使う総数</param>
    /// <returns>出現頻度結果のリスト</returns>
    public IReadOnlyList<FrequencyResult> GetResultsWithTotal(int totalForRate)
    {
        return _counts
            .Select(kvp => new FrequencyResult(
                kvp.Key,
                kvp.Value,
                totalForRate > 0 ? (double)kvp.Value / totalForRate : 0))
            .OrderByDescending(r => r.Count)
            .ThenBy(r => r.Name)
            .ToList();
    }
}