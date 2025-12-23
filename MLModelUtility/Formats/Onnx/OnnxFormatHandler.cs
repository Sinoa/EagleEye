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

using System.Runtime.InteropServices;
using Google.Protobuf;
using MLModelUtility.Capabilities;
using MLModelUtility.IO;
using MLModelUtility.Models;
using MLModelUtility.Models.Graph;
using Onnx;

namespace MLModelUtility.Formats.Onnx;

/// <summary>
/// ONNXフォーマットのハンドラ
/// </summary>
public class OnnxFormatHandler : IModelFormatHandler, ITensorReader, ITensorWriter, IGraphReader, IGraphWriter
{
    /// <inheritdoc />
    public string FormatName => "ONNX";

    /// <inheritdoc />
    public string FileExtension => ".onnx";

    /// <inheritdoc />
    public ModelFormatCapability Capability =>
        ModelFormatCapability.TensorRead | ModelFormatCapability.GraphRead | ModelFormatCapability.GraphWrite;

    #region ITensorReader Implementation

    /// <inheritdoc />
    public TensorCollection ReadTensors(Stream stream)
    {
        var model = ModelProto.Parser.ParseFrom(stream);
        return ExtractTensors(model);
    }

    /// <inheritdoc />
    public async Task<TensorCollection> ReadTensorsAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        // ストリームをメモリに読み込んでから処理
        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream, cancellationToken);
        memoryStream.Position = 0;

        return await Task.Run(() =>
        {
            var model = ModelProto.Parser.ParseFrom(memoryStream);
            return ExtractTensors(model);
        }, cancellationToken);
    }

    /// <inheritdoc />
    public TensorCollection ReadTensorsFromFile(string filePath)
    {
        using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        return ReadTensors(stream);
    }

    /// <inheritdoc />
    public async Task<TensorCollection> ReadTensorsFromFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        await using var stream = new FileStream(
            filePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 4096,
            useAsync: true);
        return await ReadTensorsAsync(stream, cancellationToken);
    }

    #endregion

    #region ITensorWriter Implementation

    /// <inheritdoc />
    public void WriteTensors(TensorCollection tensors, Stream stream)
    {
        throw new NotSupportedException("ONNX tensor-only writing is not supported. Use WriteGraphWithTensors instead.");
    }

    /// <inheritdoc />
    public Task WriteTensorsAsync(TensorCollection tensors, Stream stream, CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("ONNX tensor-only writing is not supported. Use WriteGraphWithTensorsAsync instead.");
    }

    /// <inheritdoc />
    public void WriteTensorsToFile(TensorCollection tensors, string filePath)
    {
        throw new NotSupportedException("ONNX tensor-only writing is not supported. Use WriteGraphWithTensors instead.");
    }

    /// <inheritdoc />
    public Task WriteTensorsToFileAsync(TensorCollection tensors, string filePath, CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("ONNX tensor-only writing is not supported. Use WriteGraphWithTensorsAsync instead.");
    }

    #endregion

    #region IGraphReader Implementation

    /// <inheritdoc />
    public ComputeGraph ReadGraph(Stream stream)
    {
        var model = ModelProto.Parser.ParseFrom(stream);
        return ConvertToComputeGraph(model);
    }

    /// <inheritdoc />
    public async Task<ComputeGraph> ReadGraphAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream, cancellationToken);
        memoryStream.Position = 0;

        return await Task.Run(() =>
        {
            var model = ModelProto.Parser.ParseFrom(memoryStream);
            return ConvertToComputeGraph(model);
        }, cancellationToken);
    }

    /// <inheritdoc />
    public ComputeGraph ReadGraphFromFile(string filePath)
    {
        using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        return ReadGraph(stream);
    }

    /// <inheritdoc />
    public async Task<ComputeGraph> ReadGraphFromFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        await using var stream = new FileStream(
            filePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 4096,
            useAsync: true);
        return await ReadGraphAsync(stream, cancellationToken);
    }

    /// <inheritdoc />
    public (ComputeGraph Graph, TensorCollection Tensors) ReadGraphWithTensors(Stream stream)
    {
        var model = ModelProto.Parser.ParseFrom(stream);
        var graph = ConvertToComputeGraph(model);
        var tensors = ExtractTensors(model);
        return (graph, tensors);
    }

    /// <inheritdoc />
    public async Task<(ComputeGraph Graph, TensorCollection Tensors)> ReadGraphWithTensorsAsync(
        Stream stream, CancellationToken cancellationToken = default)
    {
        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream, cancellationToken);
        memoryStream.Position = 0;

        return await Task.Run(() =>
        {
            var model = ModelProto.Parser.ParseFrom(memoryStream);
            var graph = ConvertToComputeGraph(model);
            var tensors = ExtractTensors(model);
            return (graph, tensors);
        }, cancellationToken);
    }

    #endregion

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

    #region Private Methods - Reading

    /// <summary>
    /// ONNXモデルからテンソルを抽出
    /// </summary>
    private static TensorCollection ExtractTensors(ModelProto model)
    {
        var tensors = new List<ITensorData>();

        if (model.Graph == null)
        {
            return new TensorCollection(tensors);
        }

        foreach (var initializer in model.Graph.Initializer)
        {
            var tensorData = ConvertTensorProtoToTensorData(initializer);
            if (tensorData != null)
            {
                tensors.Add(tensorData);
            }
        }

        // メタデータを抽出
        Dictionary<string, string>? metadata = null;
        if (model.MetadataProps.Count > 0)
        {
            metadata = new Dictionary<string, string>();
            foreach (var prop in model.MetadataProps)
            {
                metadata[prop.Key] = prop.Value;
            }
        }

        return new TensorCollection(tensors, metadata);
    }

    /// <summary>
    /// TensorProtoからTensorDataに変換
    /// </summary>
    private static TensorData? ConvertTensorProtoToTensorData(TensorProto tensorProto)
    {
        var dataType = TensorDataTypeExtensions.FromOnnxDataType(tensorProto.DataType);
        if (dataType == TensorDataType.Unknown)
        {
            return null;
        }

        var shape = tensorProto.Dims.ToArray();
        var info = new TensorInfo(tensorProto.Name, shape, dataType);

        byte[] data;

        // RawDataがある場合はそれを使用
        if (tensorProto.RawData != null && !tensorProto.RawData.IsEmpty)
        {
            data = tensorProto.RawData.ToByteArray();
        }
        else
        {
            // 型別のデータフィールドからバイト配列に変換
            data = ExtractDataFromTensorProto(tensorProto, dataType, info.ElementCount);
        }

        // データサイズが一致しない場合は調整
        if (data.Length != info.ByteSize)
        {
            // サイズが小さい場合はパディング
            if (data.Length < info.ByteSize)
            {
                var newData = new byte[info.ByteSize];
                data.CopyTo(newData, 0);
                data = newData;
            }
            // サイズが大きい場合は切り詰め
            else
            {
                data = data.Take((int)info.ByteSize).ToArray();
            }
        }

        return new TensorData(info, data);
    }

    /// <summary>
    /// TensorProtoの型別フィールドからバイト配列を抽出
    /// </summary>
    private static byte[] ExtractDataFromTensorProto(TensorProto tensorProto, TensorDataType dataType, long elementCount)
    {
        return dataType switch
        {
            TensorDataType.Float32 => ExtractFloatData(tensorProto, elementCount),
            TensorDataType.Float64 => ExtractDoubleData(tensorProto, elementCount),
            TensorDataType.Int32 => ExtractInt32Data(tensorProto, elementCount),
            TensorDataType.Int64 => ExtractInt64Data(tensorProto, elementCount),
            TensorDataType.UInt64 => ExtractUInt64Data(tensorProto, elementCount),
            TensorDataType.Int8 or TensorDataType.UInt8 or TensorDataType.Int16 or
                TensorDataType.UInt16 or TensorDataType.Float16 or TensorDataType.BFloat16 or
                TensorDataType.Bool => ExtractInt32AsBytes(tensorProto, dataType, elementCount),
            _ => []
        };
    }

    private static byte[] ExtractFloatData(TensorProto tensorProto, long _)
    {
        if (tensorProto.FloatData.Count == 0) return [];
        var floats = tensorProto.FloatData.ToArray();
        return MemoryMarshal.AsBytes(floats.AsSpan()).ToArray();
    }

    private static byte[] ExtractDoubleData(TensorProto tensorProto, long _)
    {
        if (tensorProto.DoubleData.Count == 0) return [];
        var doubles = tensorProto.DoubleData.ToArray();
        return MemoryMarshal.AsBytes(doubles.AsSpan()).ToArray();
    }

    private static byte[] ExtractInt32Data(TensorProto tensorProto, long _)
    {
        if (tensorProto.Int32Data.Count == 0) return [];
        var ints = tensorProto.Int32Data.ToArray();
        return MemoryMarshal.AsBytes(ints.AsSpan()).ToArray();
    }

    private static byte[] ExtractInt64Data(TensorProto tensorProto, long _)
    {
        if (tensorProto.Int64Data.Count == 0) return [];
        var longs = tensorProto.Int64Data.ToArray();
        return MemoryMarshal.AsBytes(longs.AsSpan()).ToArray();
    }

    private static byte[] ExtractUInt64Data(TensorProto tensorProto, long _)
    {
        if (tensorProto.Uint64Data.Count == 0) return [];
        var ulongs = tensorProto.Uint64Data.ToArray();
        return MemoryMarshal.AsBytes(ulongs.AsSpan()).ToArray();
    }

    private static byte[] ExtractInt32AsBytes(TensorProto tensorProto, TensorDataType dataType, long elementCount)
    {
        if (tensorProto.Int32Data.Count == 0) return [];

        var byteSize = dataType.GetByteSize();
        var result = new byte[elementCount * byteSize];

        for (int i = 0; i < Math.Min(tensorProto.Int32Data.Count, (int)elementCount); i++)
        {
            var value = tensorProto.Int32Data[i];
            var bytes = BitConverter.GetBytes(value);
            for (int j = 0; j < byteSize; j++)
            {
                result[i * byteSize + j] = bytes[j];
            }
        }

        return result;
    }

    /// <summary>
    /// ONNXモデルからComputeGraphに変換
    /// </summary>
    private static ComputeGraph ConvertToComputeGraph(ModelProto model)
    {
        var graph = new ComputeGraph
        {
            Name = model.Graph?.Name ?? "",
            DocString = model.Graph?.DocString,
            IrVersion = model.IrVersion,
            ProducerName = model.ProducerName,
            ProducerVersion = model.ProducerVersion
        };

        // Opsetバージョン
        foreach (var opset in model.OpsetImport)
        {
            graph.OpsetVersions[opset.Domain ?? ""] = opset.Version;
        }

        // メタデータ
        foreach (var prop in model.MetadataProps)
        {
            graph.Metadata[prop.Key] = prop.Value;
        }

        if (model.Graph == null)
        {
            return graph;
        }

        // ノード
        foreach (var node in model.Graph.Node)
        {
            graph.Nodes.Add(ConvertNodeProto(node));
        }

        // 入力
        foreach (var input in model.Graph.Input)
        {
            var tensorInfo = ConvertValueInfoProto(input);
            if (tensorInfo != null)
            {
                graph.Inputs.Add(tensorInfo);
            }
        }

        // 出力
        foreach (var output in model.Graph.Output)
        {
            var tensorInfo = ConvertValueInfoProto(output);
            if (tensorInfo != null)
            {
                graph.Outputs.Add(tensorInfo);
            }
        }

        // 初期化子の名前
        foreach (var initializer in model.Graph.Initializer)
        {
            graph.InitializerNames.Add(initializer.Name);
        }

        return graph;
    }

    /// <summary>
    /// NodeProtoからGraphNodeに変換
    /// </summary>
    private static GraphNode ConvertNodeProto(NodeProto nodeProto)
    {
        var node = new GraphNode
        {
            Name = nodeProto.Name,
            OperatorType = nodeProto.OpType,
            DocString = nodeProto.DocString
        };

        node.Inputs.AddRange(nodeProto.Input);
        node.Outputs.AddRange(nodeProto.Output);

        // 属性を変換
        foreach (var attr in nodeProto.Attribute)
        {
            node.Attributes[attr.Name] = ConvertAttributeValue(attr);
        }

        return node;
    }

    /// <summary>
    /// AttributeProtoから値を抽出
    /// </summary>
    private static object ConvertAttributeValue(AttributeProto attr)
    {
        return attr.Type switch
        {
            AttributeProto.Types.AttributeType.Float => attr.F,
            AttributeProto.Types.AttributeType.Int => attr.I,
            AttributeProto.Types.AttributeType.String => attr.S.ToStringUtf8(),
            AttributeProto.Types.AttributeType.Floats => attr.Floats.ToArray(),
            AttributeProto.Types.AttributeType.Ints => attr.Ints.ToArray(),
            AttributeProto.Types.AttributeType.Strings => attr.Strings.Select(s => s.ToStringUtf8()).ToArray(),
            _ => attr.ToString() ?? ""
        };
    }

    /// <summary>
    /// ValueInfoProtoからTensorInfoに変換
    /// </summary>
    private static TensorInfo? ConvertValueInfoProto(ValueInfoProto valueInfo)
    {
        if (valueInfo.Type?.TensorType == null)
        {
            return null;
        }

        var tensorType = valueInfo.Type.TensorType;
        var dataType = TensorDataTypeExtensions.FromOnnxDataType(tensorType.ElemType);

        var shape = new List<long>();
        if (tensorType.Shape != null)
        {
            foreach (var dim in tensorType.Shape.Dim)
            {
                // 動的次元の場合は-1として扱う
                shape.Add(dim.DimValue > 0 ? dim.DimValue : -1);
            }
        }

        return new TensorInfo(valueInfo.Name, shape, dataType);
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