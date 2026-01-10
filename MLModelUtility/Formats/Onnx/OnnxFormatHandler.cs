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

using Google.Protobuf;
using MLModelUtility.Capabilities;
using MLModelUtility.IO;
using MLModelUtility.Models;
using MLModelUtility.Models.Graph;
using Onnx;

namespace MLModelUtility.Formats.Onnx;

/// <summary>
/// ONNXフォーマットのハンドラ（エクスポート専用）
/// </summary>
public class OnnxFormatHandler : IModelFormatHandler, IGraphWriter
{
    /// <inheritdoc />
    public string FormatName => "ONNX";

    /// <inheritdoc />
    public string FileExtension => ".onnx";

    /// <inheritdoc />
    public ModelFormatCapability Capability => ModelFormatCapability.GraphWrite;

    #region IGraphWriter Implementation

    /// <inheritdoc />
    public void WriteGraph(ComputeGraph graph, Stream stream)
    {
        var model = ConvertToModelProto(graph, null);
        model.WriteTo(stream);
    }

    /// <inheritdoc />
    public async Task WriteGraphAsync(ComputeGraph graph, Stream stream, CancellationToken cancellationToken = default)
    {
        var model = ConvertToModelProto(graph, null);
        using var memoryStream = new MemoryStream();
        model.WriteTo(memoryStream);
        memoryStream.Position = 0;
        await memoryStream.CopyToAsync(stream, cancellationToken);
    }

    /// <inheritdoc />
    public void WriteGraphToFile(ComputeGraph graph, string filePath)
    {
        using var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
        WriteGraph(graph, stream);
    }

    /// <inheritdoc />
    public async Task WriteGraphToFileAsync(ComputeGraph graph, string filePath, CancellationToken cancellationToken = default)
    {
        await using var stream = new FileStream(
            filePath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 4096,
            useAsync: true);
        await WriteGraphAsync(graph, stream, cancellationToken);
    }

    /// <inheritdoc />
    public void WriteGraphWithTensors(ComputeGraph graph, TensorCollection tensors, Stream stream)
    {
        var model = ConvertToModelProto(graph, tensors);
        model.WriteTo(stream);
    }

    /// <inheritdoc />
    public async Task WriteGraphWithTensorsAsync(
        ComputeGraph graph, TensorCollection tensors, Stream stream, CancellationToken cancellationToken = default)
    {
        var model = ConvertToModelProto(graph, tensors);
        using var memoryStream = new MemoryStream();
        model.WriteTo(memoryStream);
        memoryStream.Position = 0;
        await memoryStream.CopyToAsync(stream, cancellationToken);
    }


    #endregion

    #region Private Methods - Writing

    /// <summary>
    /// ComputeGraphとTensorCollectionからModelProtoに変換
    /// </summary>
    private static ModelProto ConvertToModelProto(ComputeGraph computeGraph, TensorCollection? tensors)
    {
        var model = new ModelProto
        {
            IrVersion = computeGraph.IrVersion > 0 ? computeGraph.IrVersion : 8,
            ProducerName = computeGraph.ProducerName ?? "MLModelUtility",
            ProducerVersion = computeGraph.ProducerVersion ?? "1.0.0"
        };

        // Opsetバージョン
        foreach (var (domain, version) in computeGraph.OpsetVersions)
        {
            model.OpsetImport.Add(new OperatorSetIdProto
            {
                Domain = domain,
                Version = version
            });
        }

        // デフォルトのOpsetがない場合は追加
        if (model.OpsetImport.Count == 0)
        {
            model.OpsetImport.Add(new OperatorSetIdProto
            {
                Domain = "",
                Version = 17
            });
        }

        // メタデータ
        foreach (var (key, value) in computeGraph.Metadata)
        {
            model.MetadataProps.Add(new StringStringEntryProto
            {
                Key = key,
                Value = value
            });
        }

        // グラフを構築
        var graphProto = new GraphProto
        {
            Name = computeGraph.Name,
            DocString = computeGraph.DocString ?? ""
        };

        // ノード
        foreach (var node in computeGraph.Nodes)
        {
            graphProto.Node.Add(ConvertToNodeProto(node));
        }

        // 入力
        foreach (var input in computeGraph.Inputs)
        {
            graphProto.Input.Add(ConvertToValueInfoProto(input));
        }

        // 出力
        foreach (var output in computeGraph.Outputs)
        {
            graphProto.Output.Add(ConvertToValueInfoProto(output));
        }

        // テンソルデータ（初期化子）
        if (tensors != null)
        {
            foreach (var tensor in tensors)
            {
                graphProto.Initializer.Add(ConvertToTensorProto(tensor));
            }
        }

        model.Graph = graphProto;
        return model;
    }

    /// <summary>
    /// GraphNodeからNodeProtoに変換
    /// </summary>
    private static NodeProto ConvertToNodeProto(GraphNode node)
    {
        var nodeProto = new NodeProto
        {
            Name = node.Name,
            OpType = node.OperatorType,
            DocString = node.DocString ?? ""
        };

        nodeProto.Input.AddRange(node.Inputs);
        nodeProto.Output.AddRange(node.Outputs);

        // 属性を変換
        foreach (var (name, value) in node.Attributes)
        {
            var attr = ConvertToAttributeProto(name, value);
            if (attr != null)
            {
                nodeProto.Attribute.Add(attr);
            }
        }

        return nodeProto;
    }

    /// <summary>
    /// 値からAttributeProtoに変換
    /// </summary>
    private static AttributeProto? ConvertToAttributeProto(string name, object value)
    {
        var attr = new AttributeProto { Name = name };

        switch (value)
        {
            case float f:
                attr.Type = AttributeProto.Types.AttributeType.Float;
                attr.F = f;
                break;

            case double d:
                attr.Type = AttributeProto.Types.AttributeType.Float;
                attr.F = (float)d;
                break;

            case int i:
                attr.Type = AttributeProto.Types.AttributeType.Int;
                attr.I = i;
                break;

            case long l:
                attr.Type = AttributeProto.Types.AttributeType.Int;
                attr.I = l;
                break;

            case string s:
                attr.Type = AttributeProto.Types.AttributeType.String;
                attr.S = ByteString.CopyFromUtf8(s);
                break;

            case float[] floats:
                attr.Type = AttributeProto.Types.AttributeType.Floats;
                attr.Floats.AddRange(floats);
                break;

            case int[] ints:
                attr.Type = AttributeProto.Types.AttributeType.Ints;
                attr.Ints.AddRange(ints.Select(x => (long)x));
                break;

            case long[] longs:
                attr.Type = AttributeProto.Types.AttributeType.Ints;
                attr.Ints.AddRange(longs);
                break;

            case string[] strings:
                attr.Type = AttributeProto.Types.AttributeType.Strings;
                attr.Strings.AddRange(strings.Select(ByteString.CopyFromUtf8));
                break;

            default:
                return null;
        }

        return attr;
    }

    /// <summary>
    /// TensorInfoからValueInfoProtoに変換
    /// </summary>
    private static ValueInfoProto ConvertToValueInfoProto(TensorInfo tensorInfo)
    {
        var valueInfo = new ValueInfoProto
        {
            Name = tensorInfo.Name
        };

        var tensorType = new TypeProto.Types.Tensor
        {
            ElemType = tensorInfo.DataType.ToOnnxDataType()
        };

        var shape = new TensorShapeProto();
        foreach (var dim in tensorInfo.Shape)
        {
            shape.Dim.Add(new TensorShapeProto.Types.Dimension
            {
                DimValue = dim
            });
        }

        tensorType.Shape = shape;

        valueInfo.Type = new TypeProto
        {
            TensorType = tensorType
        };

        return valueInfo;
    }

    /// <summary>
    /// ITensorDataからTensorProtoに変換
    /// </summary>
    private static TensorProto ConvertToTensorProto(ITensorData tensorData)
    {
        var tensorProto = new TensorProto
        {
            Name = tensorData.Info.Name,
            DataType = tensorData.Info.DataType.ToOnnxDataType()
        };

        tensorProto.Dims.AddRange(tensorData.Info.Shape);

        // RawDataとしてバイナリデータを設定
        var dataSpan = tensorData.GetDataSpan();
        tensorProto.RawData = ByteString.CopyFrom(dataSpan);

        return tensorProto;
    }

    #endregion
}