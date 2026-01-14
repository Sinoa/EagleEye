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

namespace MjlogA.Analyzers;

/// <summary>
/// 分析器の種類を表すフラグ列挙型
/// </summary>
[Flags]
public enum AnalyzerTypes
{
    /// <summary>なし</summary>
    None = 0,

    /// <summary>和了時の点数分布</summary>
    Score = 1 << 0,

    /// <summary>局数分布</summary>
    RoundCount = 1 << 1,

    /// <summary>巡目分布</summary>
    TurnCount = 1 << 2,

    /// <summary>役の出現頻度</summary>
    Yaku = 1 << 3,

    /// <summary>ドラ牌の出現頻度</summary>
    Dora = 1 << 4,

    /// <summary>持ち点分布</summary>
    PointDistribution = 1 << 5,

    /// <summary>すべての分析器</summary>
    All = Score | RoundCount | TurnCount | Yaku | Dora | PointDistribution
}

