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

namespace MjlogReader.Models.Results;

/// <summary>
/// セッション（局）の結果
/// </summary>
public class MjlogSessionResult
{
    /// <summary>和了による終局かどうか</summary>
    public bool IsAgari { get; set; }

    /// <summary>和了結果（複数の場合はダブロン・トリロン）</summary>
    public List<AgariInfo> AgariInfos { get; set; } = [];

    /// <summary>流局結果（和了でない場合）</summary>
    public RyuukyokuInfo? RyuukyokuInfo { get; set; }

    /// <summary>終局後の各プレイヤーの得点</summary>
    public int[] FinalScores { get; set; } = new int[4];

    /// <summary>
    /// 文字列表現を取得
    /// </summary>
    public override string ToString()
    {
        if (IsAgari)
        {
            var agariStr = string.Join(", ", AgariInfos.Select(a => a.ToString()));
            return $"和了: {agariStr}";
        }

        return RyuukyokuInfo?.ToString() ?? "流局";
    }
}