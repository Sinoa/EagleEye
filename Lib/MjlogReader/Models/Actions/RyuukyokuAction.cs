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

namespace MjlogReader.Models.Actions;

/// <summary>
/// 流局行動（流局したという事実の記録のみ、詳細は MjlogSessionResult に）
/// </summary>
public class RyuukyokuAction : MjlogAction
{
    /// <inheritdoc/>
    public override ActionType ActionType => ActionType.Ryuukyoku;

    /// <summary>
    /// 文字列表現を取得
    /// </summary>
    public override string ToString() => "Ryuukyoku";
}