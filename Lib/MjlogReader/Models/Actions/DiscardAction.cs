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
/// 打牌行動
/// </summary>
public class DiscardAction : MjlogAction
{
    /// <inheritdoc/>
    public override ActionType ActionType => ActionType.Discard;

    /// <summary>捨てた牌</summary>
    public Tile? Tile { get; set; }

    /// <summary>ツモ切りかどうか</summary>
    public bool IsTsumogiri { get; set; }

    /// <summary>
    /// 文字列表現を取得
    /// </summary>
    public override string ToString()
    {
        var tsumogiriMark = IsTsumogiri ? "*" : "";
        return $"Discard({Tile?.DisplayName ?? "?"}{tsumogiriMark})";
    }
}