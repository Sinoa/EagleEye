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

using MjlogJ.Models;

namespace MjlogA.Analyzers;

/// <summary>
/// 試合の局数を分析する
/// </summary>
public class RoundCountAnalyzer
{
    private readonly List<int> _roundCounts = [];

    /// <summary>
    /// 試合データから局数を収集
    /// </summary>
    /// <param name="game">試合データ</param>
    public void Analyze(GameRecord game)
    {
        _roundCounts.Add(game.Rounds.Count);
    }

    /// <summary>
    /// 収集した局数のリストを取得
    /// </summary>
    public IReadOnlyList<int> RoundCounts => _roundCounts;
}