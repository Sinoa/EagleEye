// zlib License
// 
// Copyright (c) 2026 Sinoa
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

namespace Foxtamp.MjlogReader.Models;

/// <summary>
/// 牌譜ヘッダー情報（SHUFFLE, GO, UN 要素のデータ）
/// </summary>
public class MjlogHeader
{
    /// <summary>牌譜ID</summary>
    public string GameId { get; set; } = "";

    /// <summary>対戦日時</summary>
    public DateTime PlayedAt { get; set; }

    /// <summary>プレイヤー名リスト（席順、インデックス0-3）</summary>
    public string[] PlayerNames { get; set; } = new string[4];

    /// <summary>プレイヤーの段位</summary>
    public string[] PlayerDans { get; set; } = new string[4];

    /// <summary>プレイヤーのレート</summary>
    public float[] PlayerRates { get; set; } = new float[4];

    /// <summary>プレイヤーの性別（M=男, F=女, C=コンピュータ）</summary>
    public string[] PlayerSexes { get; set; } = new string[4];

    /// <summary>ゲームルール</summary>
    public GameRule? Rule { get; set; }

    /// <summary>牌譜のシャッフル情報（存在する場合）</summary>
    public string? ShuffleSeed { get; set; }

    /// <summary>牌譜のリファレンス情報</summary>
    public string? Reference { get; set; }
}