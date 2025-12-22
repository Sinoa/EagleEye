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

namespace MjlogJ.Models;

/// <summary>
/// ゲームルール情報
/// </summary>
public class GameRule
{
    /// <summary>赤ドラの有無</summary>
    public bool HasRedDora { get; set; }

    /// <summary>喰いタンの有無</summary>
    public bool HasOpenTanyao { get; set; }

    /// <summary>東風戦かどうか（falseなら半荘戦）</summary>
    public bool IsEastOnly { get; set; }

    /// <summary>三人麻雀かどうか</summary>
    public bool IsThreePlayer { get; set; }

    /// <summary>速度（0=普通, 1=高速, 2=超高速）</summary>
    public int Speed { get; set; }

    /// <summary>喰い替えの有無</summary>
    public bool HasKuikae { get; set; }

    /// <summary>ルールのビットフラグ（元データ）</summary>
    public int OriginalFlags { get; set; }

    /// <summary>ロビー番号</summary>
    public int Lobby { get; set; }
}

