// zlib License
// 
// Copyright (c) 2025 Sinoa
// 
// This software is provided ‘as-is’, without any express or implied
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

namespace MjlogConverter.Models;

/// <summary>
/// プレイヤー最終結果
/// </summary>
public class PlayerResult
{
    /// <summary>プレイヤーID（0-3）</summary>
    public int PlayerId { get; set; }

    /// <summary>プレイヤー名</summary>
    public string Name { get; set; } = "";

    /// <summary>最終得点</summary>
    public int FinalScore { get; set; }

    /// <summary>順位（1-4）</summary>
    public int Rank { get; set; }

    /// <summary>段位</summary>
    public string Dan { get; set; } = "";

    /// <summary>レート</summary>
    public float Rate { get; set; }

    /// <summary>性別（不明な場合は空）</summary>
    public string Sex { get; set; } = "";
}

/// <summary>
/// ゲーム最終結果
/// </summary>
public class GameResult
{
    /// <summary>各プレイヤーの結果</summary>
    public List<PlayerResult> PlayerResults { get; set; } = [];
}