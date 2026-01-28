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

using System.Text.RegularExpressions;
using Foxtamp.MLModelCodec.Models.Abstract;
using Foxtamp.MLModelCodec.Utilities;
using Google.Protobuf;
using Onnx;

namespace Foxtamp.MLModelCodec.Encoders;

/// <summary>
/// <see cref="MLAbstractModel"/>をONNX形式にエンコードするクラスです。
/// </summary>
/// <remarks>
/// <para>このクラスは以下の機能を提供します：</para>
/// <list type="bullet">
///   <item><description><see cref="Encode"/> - <see cref="MLAbstractModel"/>を<see cref="ModelProto"/>に変換</description></item>
///   <item><description><see cref="Export(MLAbstractModel, Stream)"/> - ストリームへのバイナリ出力</description></item>
///   <item><description><see cref="Export(MLAbstractModel, string)"/> - ファイルへのバイナリ出力</description></item>
/// </list>
/// </remarks>
// ReSharper disable once InconsistentNaming
public sealed partial class MLModelEncoder
{
    #region Constants

    /// <summary>
    /// 制御文字（0x00-0x1F、0x7Fを除くタブ・改行）の正規表現パターン。
    /// </summary>
    private const string ControlCharsPattern = @"[\x00-\x08\x0B\x0C\x0E-\x1F\x7F]";

    #endregion

    #region Public Methods

    /// <summary>
    /// <see cref="MLAbstractModel"/>を<see cref="ModelProto"/>にエンコードします。
    /// </summary>
    /// <param name="model">エンコードするモデル</param>
    /// <param name="opsetVersion">ONNXオペセットのバージョン（デフォルトは24）</param>
    /// <returns>変換された<see cref="ModelProto"/>インスタンス</returns>
    /// <exception cref="ArgumentNullException"><paramref name="model"/>がnullの場合</exception>
    /// <example>
    /// <code>
    /// var encoder = new MLModelOnnxEncoder();
    /// var modelProto = encoder.Encode(abstractModel);
    /// </code>
    /// </example>
    public ModelProto Encode(MLAbstractModel model, int opsetVersion = 24)
    {
        ArgumentNullException.ThrowIfNull(model);

        var graphProto = ConvertGraph(model.Graph);
        var sanitizedDocument = SanitizeDocument(model.Document);

        // メタデータに作者情報を追加
        var metadata = new Dictionary<string, string>(model.Metadata)
        {
            ["author"] = model.Author,
        };

        var modelProto = OnnxUtility.CreateModel(
            model.ProducerName,
            model.ProducerVersion,
            graphProto,
            metadata,
            opsetVersion: opsetVersion);

        modelProto.ModelVersion = model.Version;
        modelProto.DocString = sanitizedDocument;
        modelProto.Domain = model.Domain;

        return modelProto;
    }

    /// <summary>
    /// <see cref="MLAbstractModel"/>をONNXバイナリ形式でストリームに出力します。
    /// </summary>
    /// <param name="model">エクスポートするモデル</param>
    /// <param name="stream">出力先ストリーム</param>
    /// <param name="opsetVersion">ONNXオペセットのバージョン（デフォルトは24）</param>
    /// <exception cref="ArgumentNullException"><paramref name="model"/>または<paramref name="stream"/>がnullの場合</exception>
    /// <example>
    /// <code>
    /// var encoder = new MLModelOnnxEncoder();
    /// using var stream = new MemoryStream();
    /// encoder.Export(abstractModel, stream);
    /// </code>
    /// </example>
    public void Export(MLAbstractModel model, Stream stream, int opsetVersion = 24)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(stream);

        var modelProto = Encode(model, opsetVersion);
        modelProto.WriteTo(stream);
    }

    /// <summary>
    /// <see cref="MLAbstractModel"/>をONNXバイナリ形式でファイルに出力します。
    /// </summary>
    /// <param name="model">エクスポートするモデル</param>
    /// <param name="filePath">出力先ファイルパス</param>
    /// <param name="opsetVersion">ONNXオペセットのバージョン（デフォルトは24）</param>
    /// <exception cref="ArgumentNullException"><paramref name="model"/>または<paramref name="filePath"/>がnullの場合</exception>
    /// <exception cref="ArgumentException"><paramref name="filePath"/>が空文字列の場合</exception>
    /// <example>
    /// <code>
    /// var encoder = new MLModelOnnxEncoder();
    /// encoder.Export(abstractModel, "model.onnx");
    /// </code>
    /// </example>
    public void Export(MLAbstractModel model, string filePath, int opsetVersion = 24)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentException.ThrowIfNullOrEmpty(filePath);

        using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write);
        Export(model, fileStream, opsetVersion);
    }

    #endregion

    #region Graph Conversion

    /// <summary>
    /// <see cref="MLAbstractGraph"/>を<see cref="GraphProto"/>に変換します。
    /// </summary>
    /// <param name="graph">変換するグラフ</param>
    /// <returns>変換された<see cref="GraphProto"/>インスタンス</returns>
    private GraphProto ConvertGraph(MLAbstractGraph graph)
    {
        var graphProto = new GraphProto
        {
            Name = graph.Name,
        };

        // 入力の変換
        foreach (var input in graph.Inputs)
        {
            graphProto.Input.Add(ConvertValueInfo(input));
        }

        // 定数（Initializer）の変換
        foreach (var constant in graph.Constants)
        {
            graphProto.Initializer.Add(ConvertTensor(constant));
        }

        // ノードの変換
        foreach (var node in graph.Nodes)
        {
            graphProto.Node.Add(ConvertNode(node));
        }

        // 出力の変換
        foreach (var output in graph.Outputs)
        {
            graphProto.Output.Add(ConvertValueInfo(output));
        }

        return graphProto;
    }

    #endregion

    #region Node Conversion

    /// <summary>
    /// <see cref="MLAbstractNode"/>を<see cref="NodeProto"/>に変換します。
    /// </summary>
    /// <param name="node">変換するノード</param>
    /// <returns>変換された<see cref="NodeProto"/>インスタンス</returns>
    private NodeProto ConvertNode(MLAbstractNode node)
    {
        var attributes = node.Attributes
            .Select(ConvertAttribute)
            .ToArray();

        return OnnxUtility.CreateNode(
            node.Name,
            node.OpType,
            node.Inputs,
            node.Outputs,
            attributes);
    }

    #endregion

    #region Tensor Conversion

    /// <summary>
    /// <see cref="MLAbstractTensor"/>を<see cref="TensorProto"/>に変換します。
    /// </summary>
    /// <param name="tensor">変換するテンソル</param>
    /// <returns>変換された<see cref="TensorProto"/>インスタンス</returns>
    private static TensorProto ConvertTensor(MLAbstractTensor tensor)
    {
        return OnnxUtility.CreateTensor(tensor.Name, tensor.Tensor);
    }

    #endregion

    #region ValueInfo Conversion

    /// <summary>
    /// <see cref="MLAbstractValueInfo"/>を<see cref="ValueInfoProto"/>に変換します。
    /// </summary>
    /// <param name="valueInfo">変換する値情報</param>
    /// <returns>変換された<see cref="ValueInfoProto"/>インスタンス</returns>
    private static ValueInfoProto ConvertValueInfo(MLAbstractValueInfo valueInfo)
    {
        var dataType = MapDataType(valueInfo.ValueType);
        return OnnxUtility.CreateValueInfo(valueInfo.Name, valueInfo.ShapeDims, dataType);
    }

    #endregion

    #region Attribute Conversion

    /// <summary>
    /// <see cref="MLAbstractAttribute"/>を<see cref="AttributeProto"/>に変換します。
    /// </summary>
    /// <param name="attribute">変換する属性</param>
    /// <returns>変換された<see cref="AttributeProto"/>インスタンス</returns>
    /// <exception cref="NotSupportedException">サポートされていない属性値型の場合</exception>
    private static AttributeProto ConvertAttribute(MLAbstractAttribute attribute)
    {
        var value = attribute.GetValue();

        return value switch
        {
            MLAttrFloat floatAttr => OnnxUtility.CreateAttribute(attribute.Name, floatAttr.Value),
            MLAttrLong longAttr => OnnxUtility.CreateAttribute(attribute.Name, longAttr.Value),
            MLAttrString stringAttr => OnnxUtility.CreateAttribute(attribute.Name, stringAttr.Value),
            MLAttrTensor tensorAttr => OnnxUtility.CreateAttribute(attribute.Name, tensorAttr.Value),
            MLAttrFloats floatsAttr => OnnxUtility.CreateAttribute(attribute.Name, floatsAttr.Value),
            MLAttrLongs longsAttr => OnnxUtility.CreateAttribute(attribute.Name, longsAttr.Value),
            MLAttrStrings stringsAttr => OnnxUtility.CreateAttribute(attribute.Name, stringsAttr.Value),
            MLAttrTensors tensorsAttr => OnnxUtility.CreateAttribute(attribute.Name, tensorsAttr.Value),
            _ => throw new NotSupportedException($"サポートされていない属性値型です: {value.GetType().Name}"),
        };
    }

    #endregion

    #region Data Type Mapping

    /// <summary>
    /// .NET型をONNXのデータ型にマッピングします。
    /// </summary>
    /// <param name="type">マッピングする.NET型</param>
    /// <returns>対応する<see cref="TensorProto.Types.DataType"/></returns>
    /// <exception cref="NotSupportedException">サポートされていない型の場合</exception>
    /// <remarks>
    /// 現在サポートしている型：
    /// <list type="bullet">
    ///   <item><description><c>typeof(float)</c> → <see cref="TensorProto.Types.DataType.Float"/></description></item>
    /// </list>
    /// 将来的に以下の型のサポートを追加予定：
    /// <list type="bullet">
    ///   <item><description><c>typeof(double)</c> → <see cref="TensorProto.Types.DataType.Double"/></description></item>
    ///   <item><description><c>typeof(int)</c> → <see cref="TensorProto.Types.DataType.Int32"/></description></item>
    ///   <item><description><c>typeof(long)</c> → <see cref="TensorProto.Types.DataType.Int64"/></description></item>
    ///   <item><description><c>typeof(bool)</c> → <see cref="TensorProto.Types.DataType.Bool"/></description></item>
    /// </list>
    /// </remarks>
    private static TensorProto.Types.DataType MapDataType(Type type)
    {
        // 現在サポートしている型
        if (type == typeof(float))
        {
            return TensorProto.Types.DataType.Float;
        }

        // 将来の拡張用（コメントアウト）
        // if (type == typeof(double))
        // {
        //     return TensorProto.Types.DataType.Double;
        // }
        // if (type == typeof(int))
        // {
        //     return TensorProto.Types.DataType.Int32;
        // }
        // if (type == typeof(long))
        // {
        //     return TensorProto.Types.DataType.Int64;
        // }
        // if (type == typeof(bool))
        // {
        //     return TensorProto.Types.DataType.Bool;
        // }

        throw new NotSupportedException($"サポートされていないデータ型です: {type.Name}");
    }

    #endregion

    #region Document Sanitization

    /// <summary>
    /// ドキュメント文字列をサニタイズします。
    /// </summary>
    /// <remarks>
    /// <para>以下の処理を行います：</para>
    /// <list type="number">
    ///   <item><description>制御文字（タブ・改行を除く）を除去</description></item>
    /// </list>
    /// </remarks>
    /// <param name="document">サニタイズするドキュメント文字列</param>
    /// <returns>サニタイズされたドキュメント文字列</returns>
    private static string SanitizeDocument(string document)
    {
        if (string.IsNullOrEmpty(document))
        {
            return string.Empty;
        }

        // 制御文字を除去（タブ \t、改行 \n \r は保持）
        return ControlCharsRegex().Replace(document, string.Empty);
    }

    /// <summary>
    /// 制御文字検出用の正規表現を生成します。
    /// </summary>
    [GeneratedRegex(ControlCharsPattern)]
    private static partial Regex ControlCharsRegex();

    #endregion
}