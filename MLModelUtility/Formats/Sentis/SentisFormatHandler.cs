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

using Google.FlatBuffers;
using MLModelUtility.Capabilities;
using MLModelUtility.IO;
using MLModelUtility.Models;
using MLModelUtility.Models.Graph;
using SentisFlatBuffer;

namespace MLModelUtility.Formats.Sentis;

/// <summary>
/// Unity Sentis フォーマットのハンドラ
/// Sentis は Unity の推論エンジンで使用される .sentis 形式のモデルファイルを扱います。
/// このハンドラは出力のみをサポートし、読み込みはサポートしません。
/// </summary>
public class SentisFormatHandler : IModelFormatHandler, ITensorWriter, IGraphWriter
{
    /// <summary>
    /// Sentis フォーマットのバージョン
    /// </summary>
    private const uint SentisVersion = 1;

    /// <inheritdoc />
    public string FormatName => "Sentis";

    /// <inheritdoc />
    public string FileExtension => ".sentis";

    /// <inheritdoc />
    public ModelFormatCapability Capability =>
        ModelFormatCapability.TensorWrite | ModelFormatCapability.GraphWrite;

    #region ITensorWriter Implementation

    /// <inheritdoc />
    public void WriteTensors(TensorCollection tensors, Stream stream)
    {
        // テンソルのみの書き込みはサポートしないが、ダミーグラフで書き込む
        var dummyGraph = CreateDummyGraph(tensors);
        WriteGraphWithTensors(dummyGraph, tensors, stream);
    }

    /// <inheritdoc />
    public async Task WriteTensorsAsync(TensorCollection tensors, Stream stream, CancellationToken cancellationToken = default)
    {
        var dummyGraph = CreateDummyGraph(tensors);
        await WriteGraphWithTensorsAsync(dummyGraph, tensors, stream, cancellationToken);
    }

    /// <inheritdoc />
    public void WriteTensorsToFile(TensorCollection tensors, string filePath)
    {
        using var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
        WriteTensors(tensors, stream);
    }

    /// <inheritdoc />
    public async Task WriteTensorsToFileAsync(TensorCollection tensors, string filePath, CancellationToken cancellationToken = default)
    {
        await using var stream = new FileStream(
            filePath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 4096,
            useAsync: true);
        await WriteTensorsAsync(tensors, stream, cancellationToken);
    }

    #endregion

    #region IGraphWriter Implementation

    /// <inheritdoc />
    public void WriteGraph(ComputeGraph graph, Stream stream)
    {
        WriteGraphWithTensors(graph, new TensorCollection(), stream);
    }

    /// <inheritdoc />
    public async Task WriteGraphAsync(ComputeGraph graph, Stream stream, CancellationToken cancellationToken = default)
    {
        await WriteGraphWithTensorsAsync(graph, new TensorCollection(), stream, cancellationToken);
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
        var (programBytes, tensorDataBytes) = BuildSentisProgram(graph, tensors);

        // FlatBuffer データを書き込み
        stream.Write(programBytes);

        // テンソルデータを書き込み
        if (tensorDataBytes.Length > 0)
        {
            stream.Write(tensorDataBytes);
        }
    }

    /// <inheritdoc />
    public async Task WriteGraphWithTensorsAsync(
        ComputeGraph graph, TensorCollection tensors, Stream stream, CancellationToken cancellationToken = default)
    {
        var (programBytes, tensorDataBytes) = BuildSentisProgram(graph, tensors);

        // FlatBuffer データを書き込み
        await stream.WriteAsync(programBytes, cancellationToken);

        // テンソルデータを書き込み
        if (tensorDataBytes.Length > 0)
        {
            await stream.WriteAsync(tensorDataBytes, cancellationToken);
        }
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// テンソルコレクションからダミーの計算グラフを作成
    /// </summary>
    private static ComputeGraph CreateDummyGraph(TensorCollection tensors)
    {
        var graph = new ComputeGraph
        {
            Name = "TensorContainer",
            IrVersion = 1,
            ProducerName = "MLModelUtility",
            ProducerVersion = "1.0.0"
        };

        // 各テンソルを初期化子として登録
        foreach (var tensor in tensors)
        {
            graph.InitializerNames.Add(tensor.Info.Name);
        }

        return graph;
    }

    /// <summary>
    /// Sentis Program を構築
    /// </summary>
    private static (byte[] ProgramBytes, byte[] TensorDataBytes) BuildSentisProgram(
        ComputeGraph graph, TensorCollection tensors)
    {
        var builder = new FlatBufferBuilder(1024);

        // テンソルデータをバイト配列にまとめる
        var tensorDataList = new List<byte>();
        var tensorInfos = new List<(string Name, int Offset, int Length, TensorInfo Info)>();

        foreach (var tensor in tensors)
        {
            var data = tensor.GetDataSpan();
            var offset = tensorDataList.Count;
            tensorDataList.AddRange(data.ToArray());
            tensorInfos.Add((tensor.Info.Name, offset, data.Length, tensor.Info));
        }

        var tensorDataBytes = tensorDataList.ToArray();

        // EValues (テンソル) を構築
        var eValueOffsets = new List<Offset<EValue>>();
        var tensorNameToIndex = new Dictionary<string, int>();

        foreach (var (name, offset, length, info) in tensorInfos)
        {
            var tensorIndex = eValueOffsets.Count;
            tensorNameToIndex[name] = tensorIndex;

            // 形状を int[] に変換
            var fixedSizes = info.Shape.Select(s => (int)s).ToArray();
            var fixedSizesVector = SentisFlatBuffer.Tensor.CreateFixedSizesVector(builder, fixedSizes);

            // Tensor を構築
            var tensorOffset = SentisFlatBuffer.Tensor.CreateTensor(
                builder,
                (ScalarType)info.DataType.ToSentisScalarType(),
                length,
                fixedSizesVector,
                0, // constant_buffer_idx
                offset, // storage_offset
                TensorShapeDynamism.STATIC);

            // EValue を構築（Tensor を値として持つ）
            var eValueOffset = EValue.CreateEValue(
                builder,
                KernelTypes.Tensor,
                tensorOffset.Value);

            eValueOffsets.Add(eValueOffset);
        }

        // 入力テンソルの情報を追加
        var inputIndices = new List<int>();
        var inputNameOffsets = new List<StringOffset>();

        foreach (var input in graph.Inputs)
        {
            if (tensorNameToIndex.TryGetValue(input.Name, out var index))
            {
                inputIndices.Add(index);
            }
            else
            {
                // 新しいEValueを作成
                var newIndex = eValueOffsets.Count;
                inputIndices.Add(newIndex);
                tensorNameToIndex[input.Name] = newIndex;

                var fixedSizes = input.Shape.Select(s => (int)s).ToArray();
                var fixedSizesVector = SentisFlatBuffer.Tensor.CreateFixedSizesVector(builder, fixedSizes);

                var tensorOffset = SentisFlatBuffer.Tensor.CreateTensor(
                    builder,
                    (ScalarType)input.DataType.ToSentisScalarType(),
                    0,
                    fixedSizesVector,
                    0,
                    0,
                    TensorShapeDynamism.STATIC);

                var eValueOffset = EValue.CreateEValue(builder, KernelTypes.Tensor, tensorOffset.Value);
                eValueOffsets.Add(eValueOffset);
            }

            inputNameOffsets.Add(builder.CreateString(input.Name));
        }

        // 出力テンソルの情報を追加
        var outputIndices = new List<int>();
        var outputNameOffsets = new List<StringOffset>();

        foreach (var output in graph.Outputs)
        {
            if (tensorNameToIndex.TryGetValue(output.Name, out var index))
            {
                outputIndices.Add(index);
            }
            else
            {
                var newIndex = eValueOffsets.Count;
                outputIndices.Add(newIndex);
                tensorNameToIndex[output.Name] = newIndex;

                var fixedSizes = output.Shape.Select(s => (int)s).ToArray();
                var fixedSizesVector = SentisFlatBuffer.Tensor.CreateFixedSizesVector(builder, fixedSizes);

                var tensorOffset = SentisFlatBuffer.Tensor.CreateTensor(
                    builder,
                    (ScalarType)output.DataType.ToSentisScalarType(),
                    0,
                    fixedSizesVector,
                    0,
                    0,
                    TensorShapeDynamism.STATIC);

                var eValueOffset = EValue.CreateEValue(builder, KernelTypes.Tensor, tensorOffset.Value);
                eValueOffsets.Add(eValueOffset);
            }

            outputNameOffsets.Add(builder.CreateString(output.Name));
        }

        // Operators を構築
        var operatorOffsets = new List<Offset<Operator>>();
        foreach (var node in graph.Nodes)
        {
            var nameOffset = builder.CreateString(node.OperatorType);
            var operatorOffset = Operator.CreateOperator(builder, nameOffset);
            operatorOffsets.Add(operatorOffset);
        }

        // Chains を構築（単純な線形実行）
        var chainOffsets = new List<Offset<Chain>>();
        if (graph.Nodes.Count > 0)
        {
            var instructionOffsets = new List<Offset<Instruction>>();
            for (var i = 0; i < graph.Nodes.Count; i++)
            {
                var node = graph.Nodes[i];

                // 入力インデックスを取得
                var instrInputs = node.Inputs
                    .Where(tensorNameToIndex.ContainsKey)
                    .Select(name => tensorNameToIndex[name])
                    .ToArray();

                // 出力インデックスを取得/作成
                var instrOutputs = new List<int>();
                foreach (var outputName in node.Outputs)
                {
                    if (!tensorNameToIndex.TryGetValue(outputName, out var outIdx))
                    {
                        outIdx = eValueOffsets.Count;
                        tensorNameToIndex[outputName] = outIdx;

                        // ダミーのテンソルEValueを作成
                        var dummyTensorOffset = SentisFlatBuffer.Tensor.CreateTensor(
                            builder,
                            ScalarType.FLOAT,
                            0,
                            default,
                            0,
                            0,
                            TensorShapeDynamism.DYNAMIC_UNBOUND);

                        var dummyEValueOffset = EValue.CreateEValue(builder, KernelTypes.Tensor, dummyTensorOffset.Value);
                        eValueOffsets.Add(dummyEValueOffset);
                    }

                    instrOutputs.Add(outIdx);
                }

                // KernelCall を構築（入力と出力をマージしてargsに）
                var allArgs = instrInputs.Concat(instrOutputs).ToArray();
                var argsVector = KernelCall.CreateArgsVector(builder, allArgs);

                var kernelCallOffset = KernelCall.CreateKernelCall(
                    builder,
                    i, // op_index
                    argsVector);

                // Instruction を構築
                var instructionOffset = Instruction.CreateInstruction(
                    builder,
                    InstructionArguments.KernelCall,
                    kernelCallOffset.Value);

                instructionOffsets.Add(instructionOffset);
            }

            var instructionsVector = Chain.CreateInstructionsVector(builder, instructionOffsets.ToArray());
            var chainOffset = Chain.CreateChain(builder, instructionsVector);
            chainOffsets.Add(chainOffset);
        }

        // ベクターを作成
        var valuesVector = ExecutionPlan.CreateValuesVector(builder, eValueOffsets.ToArray());
        var inputsVector2 = ExecutionPlan.CreateInputsVector(builder, inputIndices.ToArray());
        var inputsNameVector = ExecutionPlan.CreateInputsNameVector(builder, inputNameOffsets.ToArray());
        var outputsVector2 = ExecutionPlan.CreateOutputsVector(builder, outputIndices.ToArray());
        var outputsNameVector = ExecutionPlan.CreateOutputsNameVector(builder, outputNameOffsets.ToArray());
        var chainsVector = ExecutionPlan.CreateChainsVector(builder, chainOffsets.ToArray());
        var operatorsVector = ExecutionPlan.CreateOperatorsVector(builder, operatorOffsets.ToArray());

        // ExecutionPlan を構築
        var nameOffset2 = builder.CreateString(graph.Name ?? "model");
        var executionPlanOffset = ExecutionPlan.CreateExecutionPlan(
            builder,
            nameOffset2,
            valuesVector,
            inputsVector2,
            inputsNameVector,
            outputsVector2,
            outputsNameVector,
            chainsVector,
            operatorsVector);

        // DataSegment を構築
        var segmentOffsets = new List<Offset<DataSegment>>();
        if (tensorDataBytes.Length > 0)
        {
            var segmentOffset = DataSegment.CreateDataSegment(
                builder,
                0, // offset (FlatBuffer直後から)
                (ulong)tensorDataBytes.Length);
            segmentOffsets.Add(segmentOffset);
        }

        var segmentsVector = Program.CreateSegmentsVector(builder, segmentOffsets.ToArray());

        // Program を構築
        var programOffset = Program.CreateProgram(
            builder,
            SentisVersion,
            executionPlanOffset,
            0, // segments_offset
            segmentsVector);

        // ファイナライズ
        Program.FinishProgramBuffer(builder, programOffset);

        var programBytes = builder.SizedByteArray();
        return (programBytes, tensorDataBytes);
    }

    #endregion
}