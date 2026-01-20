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

namespace Foxtamp.MjlogReader.Models.Actions;

/// <summary>
/// 行動の種類
/// </summary>
public enum ActionType
{
    /// <summary>ツモ（牌を引く）</summary>
    Draw,

    /// <summary>打牌（牌を捨てる）</summary>
    Discard,

    /// <summary>鳴き（チー、ポン、カン等）</summary>
    Meld,

    /// <summary>リーチ宣言</summary>
    Reach,

    /// <summary>新ドラ表示（カン後のドラめくり）</summary>
    Dora,

    /// <summary>和了</summary>
    Agari,

    /// <summary>流局</summary>
    Ryuukyoku
}