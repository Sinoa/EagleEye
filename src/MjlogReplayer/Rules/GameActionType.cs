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

namespace Foxtamp.MjlogReplayer.Rules;

/// <summary>
/// ゲームアクションの種類を表すフラグ列挙型（ARCHITECTURE §7準拠）
/// </summary>
[Flags]
public enum GameActionType
{
    /// <summary>アクションなし</summary>
    None = 0,

    /// <summary>打牌</summary>
    Discard = 1 << 0,

    /// <summary>リーチ宣言</summary>
    Riichi = 1 << 1,

    /// <summary>ツモ和了</summary>
    Tsumo = 1 << 2,

    /// <summary>ロン和了</summary>
    Ron = 1 << 3,

    /// <summary>ポン</summary>
    Pon = 1 << 4,

    /// <summary>チー</summary>
    Chi = 1 << 5,

    /// <summary>暗槓</summary>
    AnKan = 1 << 6,

    /// <summary>加槓</summary>
    KaKan = 1 << 7,

    /// <summary>大明槓</summary>
    DaiMinKan = 1 << 8,

    /// <summary>九種九牌</summary>
    KyuushuKyuuhai = 1 << 9,

    /// <summary>スキップ（鳴かない）</summary>
    Skip = 1 << 10,

    /// <summary>北抜き（三麻専用）</summary>
    Nuki = 1 << 11,
}
