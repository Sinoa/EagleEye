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

namespace MLModelUtility.Models.Graph;

/// <summary>
/// 計算グラフのノード（演算子）を表すクラス
/// </summary>
public class GraphNode
{
    /// <summary>
    /// ノードの一意な識別子
    /// </summary>
    public string Name { get; set; } = "";

    /// <summary>
    /// 演算子の種類（例: "Conv", "Relu", "MatMul"）
    /// </summary>
    public string OperatorType { get; set; } = "";

    /// <summary>
    /// 入力テンソル/ノードの名前リスト
    /// </summary>
    public List<string> Inputs { get; set; } = [];

    /// <summary>
    /// 出力テンソルの名前リスト
    /// </summary>
    public List<string> Outputs { get; set; } = [];

    /// <summary>
    /// 演算子の属性（ハイパーパラメータなど）
    /// </summary>
    public Dictionary<string, object> Attributes { get; set; } = [];

    /// <summary>
    /// ノードのドキュメント文字列
    /// </summary>
    public string? DocString { get; set; }

    public override string ToString()
    {
        return $"{Name} ({OperatorType}): [{string.Join(", ", Inputs)}] -> [{string.Join(", ", Outputs)}]";
    }
}