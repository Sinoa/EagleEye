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
/// 計算グラフ全体を表すクラス
/// </summary>
public class ComputeGraph
{
    /// <summary>
    /// グラフの名前
    /// </summary>
    public string Name { get; set; } = "";

    /// <summary>
    /// グラフのドキュメント文字列
    /// </summary>
    public string? DocString { get; set; }

    /// <summary>
    /// グラフ内のノードリスト
    /// </summary>
    public List<GraphNode> Nodes { get; set; } = [];

    /// <summary>
    /// グラフの入力テンソル情報
    /// </summary>
    public List<TensorInfo> Inputs { get; set; } = [];

    /// <summary>
    /// グラフの出力テンソル情報
    /// </summary>
    public List<TensorInfo> Outputs { get; set; } = [];

    /// <summary>
    /// 初期化子（重み等の初期値を持つテンソル）の名前リスト
    /// 実際のデータはTensorCollectionで別途管理
    /// </summary>
    public List<string> InitializerNames { get; set; } = [];

    /// <summary>
    /// モデルのIRバージョン（ONNXの場合）
    /// </summary>
    public long IrVersion { get; set; }

    /// <summary>
    /// Opsetバージョン情報
    /// </summary>
    public Dictionary<string, long> OpsetVersions { get; set; } = [];

    /// <summary>
    /// プロデューサー名
    /// </summary>
    public string? ProducerName { get; set; }

    /// <summary>
    /// プロデューサーバージョン
    /// </summary>
    public string? ProducerVersion { get; set; }

    /// <summary>
    /// カスタムメタデータ
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; } = [];

    /// <summary>
    /// 指定した名前のノードを取得
    /// </summary>
    /// <param name="name">ノード名</param>
    /// <returns>ノード（見つからない場合null）</returns>
    public GraphNode? FindNode(string name)
    {
        return Nodes.FirstOrDefault(n => n.Name == name);
    }

    /// <summary>
    /// 指定した演算子タイプのノードを全て取得
    /// </summary>
    /// <param name="operatorType">演算子タイプ</param>
    /// <returns>該当するノードのリスト</returns>
    public IEnumerable<GraphNode> FindNodesByOperator(string operatorType)
    {
        return Nodes.Where(n => n.OperatorType == operatorType);
    }

    public override string ToString()
    {
        return $"{Name}: {Nodes.Count} nodes, {Inputs.Count} inputs, {Outputs.Count} outputs";
    }
}