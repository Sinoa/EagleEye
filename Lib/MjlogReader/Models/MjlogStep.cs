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

using MjlogReader.Models.Actions;

namespace MjlogReader.Models;

/// <summary>
/// 牌譜ステップ（1つの行動を表す）
/// </summary>
public class MjlogStep
{
    /// <summary>ステップインデックス（セッション内での0から始まる連番）</summary>
    public int StepIndex { get; set; }

    /// <summary>巡目（1から始まる、親の打牌完了で1巡、鳴きによる順番スキップも考慮）</summary>
    public int TurnNumber { get; set; }

    /// <summary>行動したプレイヤーID（0-3、システム行動の場合は-1）</summary>
    public int PlayerId { get; set; }

    /// <summary>行動内容</summary>
    public MjlogAction Action { get; set; } = null!;

    /// <summary>
    /// 文字列表現を取得
    /// </summary>
    public override string ToString()
    {
        var playerStr = PlayerId >= 0 ? $"P{PlayerId}" : "SYS";
        return $"[{StepIndex}] Turn{TurnNumber} {playerStr}: {Action}";
    }
}