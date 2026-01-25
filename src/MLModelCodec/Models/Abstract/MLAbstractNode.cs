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

namespace Foxtamp.MLModelCodec.Models.Abstract;

/// <summary>
/// ONNX計算グラフの演算ノードを表現するクラスです。
/// </summary>
/// <remarks>
/// このクラスはONNXのNodeProtoに対応し、単一の演算操作を表現します。
/// </remarks>
// ReSharper disable once InconsistentNaming
public sealed class MLAbstractNode
{
    /// <summary>
    /// ノードの識別名を取得します。
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// 演算子の種類を取得します。
    /// </summary>
    /// <remarks>
    /// ONNXオペレータ名（例: "MatMul", "Add", "Relu"）を指定します。
    /// </remarks>
    public string OpType { get; }

    /// <summary>
    /// 入力テンソル名のリストを取得します。
    /// </summary>
    public string[] Inputs { get; }

    /// <summary>
    /// 出力テンソル名のリストを取得します。
    /// </summary>
    public string[] Outputs { get; }

    /// <summary>
    /// ノードの属性リストを取得します。
    /// </summary>
    public MLAbstractAttribute[] Attributes { get; }

    /// <summary>
    /// <see cref="MLAbstractNode"/>クラスの新しいインスタンスを初期化します。
    /// </summary>
    /// <param name="name">ノードの識別名</param>
    /// <param name="opType">演算子の種類</param>
    /// <param name="inputs">入力テンソル名のリスト</param>
    /// <param name="outputs">出力テンソル名のリスト</param>
    /// <param name="attributes">ノードの属性リスト</param>
    public MLAbstractNode(string name, string opType, string[] inputs, string[] outputs, params MLAbstractAttribute[] attributes)
    {
        Name = name;
        OpType = opType;
        Inputs = inputs;
        Outputs = outputs;
        Attributes = attributes;
    }
}