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

using MLModelUtility.Capabilities;
using MLModelUtility.IO;
using MLModelUtility.Models;
using MLModelUtility.Models.Graph;

namespace MLModelUtility.Formats.Onnx;

/// <summary>
/// ONNXフォーマットのハンドラ
/// 注意: 現在は暫定実装であり、すべてのメソッドはNotImplementedExceptionをスローします
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
        throw new NotImplementedException("ONNX tensor reading is not yet implemented.");
    }

    /// <inheritdoc />
    public Task<TensorCollection> ReadTensorsAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("ONNX tensor reading is not yet implemented.");
    }

    /// <inheritdoc />
    public TensorCollection ReadTensorsFromFile(string filePath)
    {
        throw new NotImplementedException("ONNX tensor reading is not yet implemented.");
    }

    /// <inheritdoc />
    public Task<TensorCollection> ReadTensorsFromFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("ONNX tensor reading is not yet implemented.");
    }

    #endregion

    #region ITensorWriter Implementation

    /// <inheritdoc />
    public void WriteTensors(TensorCollection tensors, Stream stream)
    {
        throw new NotImplementedException("ONNX tensor-only writing is not supported. Use WriteGraphWithTensors instead.");
    }

    /// <inheritdoc />
    public Task WriteTensorsAsync(TensorCollection tensors, Stream stream, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("ONNX tensor-only writing is not supported. Use WriteGraphWithTensorsAsync instead.");
    }

    /// <inheritdoc />
    public void WriteTensorsToFile(TensorCollection tensors, string filePath)
    {
        throw new NotImplementedException("ONNX tensor-only writing is not supported. Use WriteGraphWithTensors instead.");
    }

    /// <inheritdoc />
    public Task WriteTensorsToFileAsync(TensorCollection tensors, string filePath, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("ONNX tensor-only writing is not supported. Use WriteGraphWithTensorsAsync instead.");
    }

    #endregion

    #region IGraphReader Implementation

    /// <inheritdoc />
    public ComputeGraph ReadGraph(Stream stream)
    {
        throw new NotImplementedException("ONNX graph reading is not yet implemented.");
    }

    /// <inheritdoc />
    public Task<ComputeGraph> ReadGraphAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("ONNX graph reading is not yet implemented.");
    }

    /// <inheritdoc />
    public ComputeGraph ReadGraphFromFile(string filePath)
    {
        throw new NotImplementedException("ONNX graph reading is not yet implemented.");
    }

    /// <inheritdoc />
    public Task<ComputeGraph> ReadGraphFromFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("ONNX graph reading is not yet implemented.");
    }

    /// <inheritdoc />
    public (ComputeGraph Graph, TensorCollection Tensors) ReadGraphWithTensors(Stream stream)
    {
        throw new NotImplementedException("ONNX graph reading is not yet implemented.");
    }

    /// <inheritdoc />
    public Task<(ComputeGraph Graph, TensorCollection Tensors)> ReadGraphWithTensorsAsync(
        Stream stream, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("ONNX graph reading is not yet implemented.");
    }

    #endregion

    #region IGraphWriter Implementation

    /// <inheritdoc />
    public void WriteGraph(ComputeGraph graph, Stream stream)
    {
        throw new NotImplementedException("ONNX graph writing is not yet implemented.");
    }

    /// <inheritdoc />
    public Task WriteGraphAsync(ComputeGraph graph, Stream stream, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("ONNX graph writing is not yet implemented.");
    }

    /// <inheritdoc />
    public void WriteGraphToFile(ComputeGraph graph, string filePath)
    {
        throw new NotImplementedException("ONNX graph writing is not yet implemented.");
    }

    /// <inheritdoc />
    public Task WriteGraphToFileAsync(ComputeGraph graph, string filePath, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("ONNX graph writing is not yet implemented.");
    }

    /// <inheritdoc />
    public void WriteGraphWithTensors(ComputeGraph graph, TensorCollection tensors, Stream stream)
    {
        throw new NotImplementedException("ONNX graph writing is not yet implemented.");
    }

    /// <inheritdoc />
    public Task WriteGraphWithTensorsAsync(
        ComputeGraph graph, TensorCollection tensors, Stream stream, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("ONNX graph writing is not yet implemented.");
    }

    #endregion
}