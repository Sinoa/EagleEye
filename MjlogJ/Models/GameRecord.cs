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
/// 試合全体の記録
/// </summary>
public class GameRecord
{
    /// <summary>牌譜ID</summary>
    public string GameId { get; set; } = "";

    /// <summary>対戦日時</summary>
    public DateTime PlayedAt { get; set; }

    /// <summary>プレイヤー名リスト（席順）</summary>
    public string[] PlayerNames { get; set; } = new string[4];

    /// <summary>プレイヤーの段位</summary>
    public string[] PlayerDans { get; set; } = new string[4];

    /// <summary>プレイヤーのレート</summary>
    public float[] PlayerRates { get; set; } = new float[4];

    /// <summary>ゲームルール</summary>
    public GameRule? Rule { get; set; }

    /// <summary>局のリスト</summary>
    public List<RoundRecord> Rounds { get; set; } = [];

    /// <summary>最終結果</summary>
    public GameResult? Result { get; set; }

    /// <summary>牌譜のシャッフル情報（存在する場合）</summary>
    public string? ShuffleSeed { get; set; }

    /// <summary>牌譜のリファレンス情報</summary>
    public string? Reference { get; set; }
}

