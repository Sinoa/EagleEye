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

namespace Foxtamp.MLModelExporter.Models.Abstract;

/// <summary>
/// ONNX計算グラフ構造を表現するクラスです。
/// </summary>
/// <remarks>
/// <para>このクラスはONNXのGraphProtoに対応し、以下の要素を集約します：</para>
/// <list type="bullet">
///   <item><description><see cref="Inputs"/> - グラフへの入力定義</description></item>
///   <item><description><see cref="Outputs"/> - グラフからの出力定義</description></item>
///   <item><description><see cref="Constants"/> - 学習済み重みや定数（ONNXのInitializer）</description></item>
///   <item><description><see cref="Nodes"/> - 演算ノードのリスト</description></item>
/// </list>
/// </remarks>
// ReSharper disable once InconsistentNaming
public sealed class MLAbstractGraph
{
    /// <summary>
    /// グラフ名を取得します。
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// グラフの入力定義を取得します。
    /// </summary>
    public MLAbstractValueInfo[] Inputs { get; }

    /// <summary>
    /// グラフの出力定義を取得します。
    /// </summary>
    public MLAbstractValueInfo[] Outputs { get; }

    /// <summary>
    /// モデルで持つ学習済み重みや定数パラメータのテンソルを取得します。
    /// </summary>
    /// <remarks>
    /// ONNXのInitializerに対応します。
    /// </remarks>
    public MLAbstractTensor[] Constants { get; }

    /// <summary>
    /// 演算ノードのリストを取得します。
    /// </summary>
    public MLAbstractNode[] Nodes { get; }

    /// <summary>
    /// <see cref="MLAbstractGraph"/>クラスの新しいインスタンスを初期化します。
    /// </summary>
    /// <param name="name">グラフ名</param>
    /// <param name="inputs">グラフの入力定義</param>
    /// <param name="outputs">グラフの出力定義</param>
    /// <param name="constants">学習済み重みや定数パラメータのテンソル</param>
    /// <param name="nodes">演算ノードのリスト</param>
    public MLAbstractGraph(string name, MLAbstractValueInfo[] inputs, MLAbstractValueInfo[] outputs, MLAbstractTensor[] constants, MLAbstractNode[] nodes)
    {
        Name = name;
        Inputs = inputs;
        Outputs = outputs;
        Constants = constants;
        Nodes = nodes;
    }
}