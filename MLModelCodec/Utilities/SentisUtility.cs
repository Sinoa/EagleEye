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

using System.Runtime.InteropServices;
using Google.FlatBuffers;
using SentisFlatBuffer;
using Buffer = SentisFlatBuffer.Buffer;
using Byte = SentisFlatBuffer.Byte;
using String = SentisFlatBuffer.String;

namespace MLModelCodec.Utilities;

/// <summary>
/// Sentisモデル（.sentis形式）の構築に必要なFlatBufferオブジェクトを生成するユーティリティクラスです。
/// </summary>
/// <remarks>
/// <para>このクラスは以下の機能を提供します：</para>
/// <list type="bullet">
///   <item><description><see cref="Program"/> - Sentisモデルのルートオブジェクト</description></item>
///   <item><description><see cref="ExecutionPlan"/> - 実行計画（グラフ構造）</description></item>
///   <item><description><see cref="Chain"/> - 演算命令のチェーン</description></item>
///   <item><description><see cref="Instruction"/> - 個別の演算命令</description></item>
///   <item><description><see cref="EValue"/> - テンソルや属性値のラッパー</description></item>
///   <item><description><see cref="Tensor"/> - テンソルデータ構造</description></item>
/// </list>
/// </remarks>
public static class SentisUtility
{
    /// <summary>
    /// 現在のSentisフォーマットバージョン
    /// </summary>
    private const uint CurrentVersion = 1;

    #region Program / ExecutionPlan

    /// <summary>
    /// Sentisモデルを表す<see cref="Program"/>を作成します。
    /// </summary>
    /// <param name="builder">FlatBufferBuilder</param>
    /// <param name="executionPlanOffset">実行計画のオフセット</param>
    /// <param name="segmentOffsets">データセグメントのオフセット配列</param>
    /// <param name="segmentsOffset">セグメントデータの開始オフセット（バイト位置）</param>
    /// <param name="version">フォーマットバージョン（デフォルト: 1）</param>
    /// <returns>構成された<see cref="Program"/>のオフセット</returns>
    /// <example>
    /// <code>
    /// var builder = new FlatBufferBuilder(1024);
    /// var execPlan = SentisUtility.CreateExecutionPlan(builder, ...);
    /// var program = SentisUtility.CreateProgram(builder, execPlan);
    /// </code>
    /// </example>
    public static Offset<Program> CreateProgram(
        FlatBufferBuilder builder,
        Offset<ExecutionPlan> executionPlanOffset,
        Offset<DataSegment>[]? segmentOffsets = null,
        uint segmentsOffset = 0,
        uint version = CurrentVersion)
    {
        var segmentsVector = segmentOffsets != null ? Program.CreateSegmentsVector(builder, segmentOffsets) : default;
        return Program.CreateProgram(builder, version, executionPlanOffset, segmentsOffset, segmentsVector);
    }

    /// <summary>
    /// 実行計画を表す<see cref="ExecutionPlan"/>を作成します。
    /// </summary>
    /// <param name="builder">FlatBufferBuilder</param>
    /// <param name="name">実行計画の名前</param>
    /// <param name="valueOffsets">EValue配列のオフセット</param>
    /// <param name="inputIndices">入力テンソルのインデックス配列</param>
    /// <param name="inputNames">入力テンソルの名前配列</param>
    /// <param name="outputIndices">出力テンソルのインデックス配列</param>
    /// <param name="outputNames">出力テンソルの名前配列</param>
    /// <param name="chainOffsets">Chainのオフセット配列</param>
    /// <param name="operatorOffsets">Operatorのオフセット配列</param>
    /// <param name="backendPartitioningOffset">バックエンドパーティショニングのオフセット</param>
    /// <param name="symbolicDimNames">シンボリック次元名の配列</param>
    /// <returns>構成された<see cref="ExecutionPlan"/>のオフセット</returns>
    /// <example>
    /// <code>
    /// var values = new[] { inputEValue, weightEValue, outputEValue };
    /// var valueOffsets = values.Select(v => v).ToArray();
    /// var execPlan = SentisUtility.CreateExecutionPlan(builder, "main",
    ///     valueOffsets, new[] { 0 }, new[] { "input" },
    ///     new[] { 2 }, new[] { "output" }, chainOffsets, operatorOffsets);
    /// </code>
    /// </example>
    public static Offset<ExecutionPlan> CreateExecutionPlan(
        FlatBufferBuilder builder,
        string name,
        Offset<EValue>[] valueOffsets,
        int[] inputIndices,
        string[] inputNames,
        int[] outputIndices,
        string[] outputNames,
        Offset<Chain>[] chainOffsets,
        Offset<Operator>[] operatorOffsets,
        Offset<BackendPartitioning>? backendPartitioningOffset = null,
        string[]? symbolicDimNames = null)
    {
        var nameOffset = builder.CreateString(name);
        var valuesVector = ExecutionPlan.CreateValuesVector(builder, valueOffsets);
        var inputsVector = ExecutionPlan.CreateInputsVector(builder, inputIndices);
        var inputNamesVector = ExecutionPlan.CreateInputsNameVector(builder, inputNames.Select(builder.CreateString).ToArray());
        var outputsVector = ExecutionPlan.CreateOutputsVector(builder, outputIndices);
        var outputNamesVector = ExecutionPlan.CreateOutputsNameVector(builder, outputNames.Select(builder.CreateString).ToArray());
        var chainsVector = ExecutionPlan.CreateChainsVector(builder, chainOffsets);
        var operatorsVector = ExecutionPlan.CreateOperatorsVector(builder, operatorOffsets);

        var symbolicDimNamesVector = symbolicDimNames != null
            ? ExecutionPlan.CreateSymbolicDimNamesVector(builder, symbolicDimNames.Select(builder.CreateString).ToArray())
            : default;

        return ExecutionPlan.CreateExecutionPlan(
            builder,
            nameOffset,
            valuesVector,
            inputsVector,
            inputNamesVector,
            outputsVector,
            outputNamesVector,
            chainsVector,
            operatorsVector,
            backendPartitioningOffset ?? default,
            symbolicDimNamesVector);
    }

    #endregion

    #region Operator / KernelCall / Instruction / Chain

    /// <summary>
    /// オペレーターを表す<see cref="Operator"/>を作成します。
    /// </summary>
    /// <param name="builder">FlatBufferBuilder</param>
    /// <param name="name">オペレーター名（例: "MatMul", "Add", "Relu"）</param>
    /// <returns>構成された<see cref="Operator"/>のオフセット</returns>
    /// <example>
    /// <code>
    /// var matmulOp = SentisUtility.CreateOperator(builder, "MatMul");
    /// var reluOp = SentisUtility.CreateOperator(builder, "Relu");
    /// </code>
    /// </example>
    public static Offset<Operator> CreateOperator(FlatBufferBuilder builder, string name)
    {
        var nameOffset = builder.CreateString(name);
        return Operator.CreateOperator(builder, nameOffset);
    }

    /// <summary>
    /// カーネル呼び出しを表す<see cref="KernelCall"/>を作成します。
    /// </summary>
    /// <param name="builder">FlatBufferBuilder</param>
    /// <param name="opIndex">オペレーターのインデックス（ExecutionPlan.Operatorsへの参照）</param>
    /// <param name="args">引数のインデックス配列（ExecutionPlan.Valuesへの参照）</param>
    /// <returns>構成された<see cref="KernelCall"/>のオフセット</returns>
    /// <example>
    /// <code>
    /// // MatMul(input, weight) -> output の場合
    /// // args = [inputIdx, weightIdx, outputIdx]
    /// var kernelCall = SentisUtility.CreateKernelCall(builder, 0, new[] { 0, 1, 2 });
    /// </code>
    /// </example>
    public static Offset<KernelCall> CreateKernelCall(FlatBufferBuilder builder, int opIndex, int[] args)
    {
        var argsVector = KernelCall.CreateArgsVector(builder, args);
        return KernelCall.CreateKernelCall(builder, opIndex, argsVector);
    }

    /// <summary>
    /// 命令を表す<see cref="Instruction"/>を作成します。
    /// </summary>
    /// <param name="builder">FlatBufferBuilder</param>
    /// <param name="kernelCallOffset">KernelCallのオフセット</param>
    /// <returns>構成された<see cref="Instruction"/>のオフセット</returns>
    /// <example>
    /// <code>
    /// var kernelCall = SentisUtility.CreateKernelCall(builder, 0, args);
    /// var instruction = SentisUtility.CreateInstruction(builder, kernelCall);
    /// </code>
    /// </example>
    public static Offset<Instruction> CreateInstruction(FlatBufferBuilder builder, Offset<KernelCall> kernelCallOffset)
    {
        return Instruction.CreateInstruction(
            builder,
            InstructionArguments.KernelCall,
            kernelCallOffset.Value);
    }

    /// <summary>
    /// 演算チェーンを表す<see cref="Chain"/>を作成します。
    /// </summary>
    /// <param name="builder">FlatBufferBuilder</param>
    /// <param name="inputs">入力値のインデックス配列</param>
    /// <param name="outputs">出力値のインデックス配列</param>
    /// <param name="instructionOffsets">命令のオフセット配列</param>
    /// <returns>構成された<see cref="Chain"/>のオフセット</returns>
    /// <example>
    /// <code>
    /// var instructions = new[] { instruction1, instruction2 };
    /// var chain = SentisUtility.CreateChain(builder, new[] { 0 }, new[] { 2 }, instructions);
    /// </code>
    /// </example>
    public static Offset<Chain> CreateChain(
        FlatBufferBuilder builder,
        int[] inputs,
        int[] outputs,
        Offset<Instruction>[] instructionOffsets)
    {
        var inputsVector = Chain.CreateInputsVector(builder, inputs);
        var outputsVector = Chain.CreateOutputsVector(builder, outputs);
        var instructionsVector = Chain.CreateInstructionsVector(builder, instructionOffsets);

        return Chain.CreateChain(builder, inputsVector, outputsVector, instructionsVector);
    }

    /// <summary>
    /// バックエンドパーティショニングを表す<see cref="BackendPartitioning"/>を作成します。
    /// </summary>
    /// <param name="builder">FlatBufferBuilder</param>
    /// <param name="chainIndices">チェーンのインデックス配列</param>
    /// <param name="backend">バックエンドタイプ（デフォルト: CPU）</param>
    /// <returns>構成された<see cref="BackendPartitioning"/>のオフセット</returns>
    public static Offset<BackendPartitioning> CreateBackendPartitioning(
        FlatBufferBuilder builder,
        int[] chainIndices,
        BackendType backend = BackendType.CPU)
    {
        var chainsVector = BackendPartitioning.CreateChainsVector(builder, chainIndices);
        return BackendPartitioning.CreateBackendPartitioning(builder, chainsVector, backend);
    }

    #endregion

    #region EValue - Scalar Values

    /// <summary>
    /// 整数値の<see cref="EValue"/>を作成します。
    /// </summary>
    /// <param name="builder">FlatBufferBuilder</param>
    /// <param name="value">整数値</param>
    /// <returns>構成された<see cref="EValue"/>のオフセット</returns>
    /// <example>
    /// <code>
    /// var axisValue = SentisUtility.CreateEValue(builder, 1);
    /// </code>
    /// </example>
    public static Offset<EValue> CreateEValue(FlatBufferBuilder builder, int value)
    {
        var intOffset = Int.CreateInt(builder, value);
        return EValue.CreateEValue(builder, KernelTypes.Int, intOffset.Value);
    }

    /// <summary>
    /// 浮動小数点値の<see cref="EValue"/>を作成します。
    /// </summary>
    /// <param name="builder">FlatBufferBuilder</param>
    /// <param name="value">浮動小数点値</param>
    /// <returns>構成された<see cref="EValue"/>のオフセット</returns>
    /// <example>
    /// <code>
    /// var epsilonValue = SentisUtility.CreateEValue(builder, 1e-5f);
    /// </code>
    /// </example>
    public static Offset<EValue> CreateEValue(FlatBufferBuilder builder, float value)
    {
        var floatOffset = Float.CreateFloat(builder, value);
        return EValue.CreateEValue(builder, KernelTypes.Float, floatOffset.Value);
    }

    /// <summary>
    /// ブール値の<see cref="EValue"/>を作成します。
    /// </summary>
    /// <param name="builder">FlatBufferBuilder</param>
    /// <param name="value">ブール値</param>
    /// <returns>構成された<see cref="EValue"/>のオフセット</returns>
    /// <example>
    /// <code>
    /// var keepDimsValue = SentisUtility.CreateEValue(builder, true);
    /// </code>
    /// </example>
    public static Offset<EValue> CreateEValue(FlatBufferBuilder builder, bool value)
    {
        var boolOffset = Bool.CreateBool(builder, value);
        return EValue.CreateEValue(builder, KernelTypes.Bool, boolOffset.Value);
    }

    /// <summary>
    /// バイト値の<see cref="EValue"/>を作成します。
    /// </summary>
    /// <param name="builder">FlatBufferBuilder</param>
    /// <param name="value">バイト値</param>
    /// <returns>構成された<see cref="EValue"/>のオフセット</returns>
    public static Offset<EValue> CreateEValue(FlatBufferBuilder builder, byte value)
    {
        var byteOffset = Byte.CreateByte(builder, value);
        return EValue.CreateEValue(builder, KernelTypes.Byte, byteOffset.Value);
    }

    /// <summary>
    /// 文字列値の<see cref="EValue"/>を作成します。
    /// </summary>
    /// <param name="builder">FlatBufferBuilder</param>
    /// <param name="value">文字列値</param>
    /// <returns>構成された<see cref="EValue"/>のオフセット</returns>
    /// <example>
    /// <code>
    /// var activationType = SentisUtility.CreateEValue(builder, "relu");
    /// </code>
    /// </example>
    public static Offset<EValue> CreateEValue(FlatBufferBuilder builder, string value)
    {
        var stringValOffset = builder.CreateString(value);
        var stringOffset = String.CreateString(builder, stringValOffset);
        return EValue.CreateEValue(builder, KernelTypes.String, stringOffset.Value);
    }

    /// <summary>
    /// Null値の<see cref="EValue"/>を作成します。
    /// </summary>
    /// <param name="builder">FlatBufferBuilder</param>
    /// <returns>構成された<see cref="EValue"/>のオフセット</returns>
    public static Offset<EValue> CreateNullEValue(FlatBufferBuilder builder)
    {
        Null.StartNull(builder);
        var nullOffset = Null.EndNull(builder);
        return EValue.CreateEValue(builder, KernelTypes.Null, nullOffset.Value);
    }

    #endregion

    #region EValue - Array Values

    /// <summary>
    /// 整数配列の<see cref="EValue"/>を作成します。
    /// </summary>
    /// <param name="builder">FlatBufferBuilder</param>
    /// <param name="values">整数配列</param>
    /// <returns>構成された<see cref="EValue"/>のオフセット</returns>
    /// <example>
    /// <code>
    /// var kernelShape = SentisUtility.CreateEValue(builder, new[] { 3, 3 });
    /// </code>
    /// </example>
    public static Offset<EValue> CreateEValue(FlatBufferBuilder builder, int[] values)
    {
        var itemsVector = IntList.CreateItemsVector(builder, values);
        var intListOffset = IntList.CreateIntList(builder, itemsVector);
        return EValue.CreateEValue(builder, KernelTypes.IntList, intListOffset.Value);
    }

    /// <summary>
    /// 浮動小数点配列の<see cref="EValue"/>を作成します。
    /// </summary>
    /// <param name="builder">FlatBufferBuilder</param>
    /// <param name="values">浮動小数点配列</param>
    /// <returns>構成された<see cref="EValue"/>のオフセット</returns>
    /// <example>
    /// <code>
    /// var scales = SentisUtility.CreateEValue(builder, new[] { 1.0f, 2.0f, 3.0f });
    /// </code>
    /// </example>
    public static Offset<EValue> CreateEValue(FlatBufferBuilder builder, float[] values)
    {
        var itemsVector = FloatList.CreateItemsVector(builder, values);
        var floatListOffset = FloatList.CreateFloatList(builder, itemsVector);
        return EValue.CreateEValue(builder, KernelTypes.FloatList, floatListOffset.Value);
    }

    /// <summary>
    /// ブール配列の<see cref="EValue"/>を作成します。
    /// </summary>
    /// <param name="builder">FlatBufferBuilder</param>
    /// <param name="values">ブール配列</param>
    /// <returns>構成された<see cref="EValue"/>のオフセット</returns>
    public static Offset<EValue> CreateEValue(FlatBufferBuilder builder, bool[] values)
    {
        var itemsVector = BoolList.CreateItemsVector(builder, values);
        var boolListOffset = BoolList.CreateBoolList(builder, itemsVector);
        return EValue.CreateEValue(builder, KernelTypes.BoolList, boolListOffset.Value);
    }

    #endregion

    #region EValue - Tensor

    /// <summary>
    /// テンソルの<see cref="EValue"/>を作成します。
    /// </summary>
    /// <param name="builder">FlatBufferBuilder</param>
    /// <param name="tensorOffset">Tensorのオフセット</param>
    /// <returns>構成された<see cref="EValue"/>のオフセット</returns>
    /// <example>
    /// <code>
    /// var tensor = SentisUtility.CreateTensor(builder, new[] { 1, 10 }, ScalarType.FLOAT);
    /// var tensorEValue = SentisUtility.CreateEValue(builder, tensor);
    /// </code>
    /// </example>
    public static Offset<EValue> CreateEValue(FlatBufferBuilder builder, Offset<Tensor> tensorOffset)
    {
        return EValue.CreateEValue(builder, KernelTypes.Tensor, tensorOffset.Value);
    }

    #endregion

    #region Tensor - Creation Methods

    /// <summary>
    /// 形状情報のみを持つ<see cref="Tensor"/>を作成します（入出力テンソル用）。
    /// </summary>
    /// <param name="builder">FlatBufferBuilder</param>
    /// <param name="shape">テンソルの形状</param>
    /// <param name="scalarType">スカラー型（デフォルト: FLOAT）</param>
    /// <param name="shapeDynamism">形状の動的性（デフォルト: STATIC）</param>
    /// <returns>構成された<see cref="Tensor"/>のオフセット</returns>
    /// <example>
    /// <code>
    /// // 入力テンソル（バッチ x 10次元）
    /// var inputTensor = SentisUtility.CreateTensor(builder, new[] { 1, 10 });
    /// </code>
    /// </example>
    public static Offset<Tensor> CreateTensor(
        FlatBufferBuilder builder,
        int[] shape,
        ScalarType scalarType = ScalarType.FLOAT,
        TensorShapeDynamism shapeDynamism = TensorShapeDynamism.STATIC)
    {
        var fixedSizesVector = Tensor.CreateFixedSizesVector(builder, shape);
        var lengthByte = CalculateTensorLengthByte(shape, scalarType);

        return Tensor.CreateTensor(
            builder,
            scalarType,
            lengthByte,
            fixedSizesVector,
            0,
            0,
            shapeDynamism);
    }

    /// <summary>
    /// 定数バッファを参照する<see cref="Tensor"/>を作成します（重み、バイアス用）。
    /// </summary>
    /// <param name="builder">FlatBufferBuilder</param>
    /// <param name="shape">テンソルの形状</param>
    /// <param name="constantBufferIdx">定数バッファのインデックス</param>
    /// <param name="storageOffset">バッファ内のオフセット（バイト）</param>
    /// <param name="scalarType">スカラー型（デフォルト: FLOAT）</param>
    /// <returns>構成された<see cref="Tensor"/>のオフセット</returns>
    /// <example>
    /// <code>
    /// // 重みテンソル（10 x 5）、バッファインデックス1、オフセット0
    /// var weightTensor = SentisUtility.CreateTensor(builder, new[] { 10, 5 }, 1, 0);
    /// </code>
    /// </example>
    public static Offset<Tensor> CreateTensor(
        FlatBufferBuilder builder,
        int[] shape,
        uint constantBufferIdx,
        int storageOffset,
        ScalarType scalarType = ScalarType.FLOAT)
    {
        var fixedSizesVector = Tensor.CreateFixedSizesVector(builder, shape);
        var lengthByte = CalculateTensorLengthByte(shape, scalarType);

        return Tensor.CreateTensor(
            builder,
            scalarType,
            lengthByte,
            fixedSizesVector,
            constantBufferIdx,
            storageOffset);
    }

    /// <summary>
    /// 動的次元を持つ<see cref="Tensor"/>を作成します。
    /// </summary>
    /// <param name="builder">FlatBufferBuilder</param>
    /// <param name="fixedShape">固定形状（動的次元は-1または0で指定）</param>
    /// <param name="dynamicDimOffsets">動的次元のEDimオフセット配列</param>
    /// <param name="scalarType">スカラー型（デフォルト: FLOAT）</param>
    /// <param name="hasDynamicRank">ランクが動的かどうか</param>
    /// <returns>構成された<see cref="Tensor"/>のオフセット</returns>
    public static Offset<Tensor> CreateDynamicTensor(
        FlatBufferBuilder builder,
        int[] fixedShape,
        Offset<EDim>[] dynamicDimOffsets,
        ScalarType scalarType = ScalarType.FLOAT,
        bool hasDynamicRank = false)
    {
        var fixedSizesVector = Tensor.CreateFixedSizesVector(builder, fixedShape);
        var dynamicSizesVector = Tensor.CreateDynamicSizesVector(builder, dynamicDimOffsets);

        return Tensor.CreateTensor(
            builder,
            scalarType,
            0,
            fixedSizesVector,
            0,
            0,
            TensorShapeDynamism.DYNAMIC_UNBOUND,
            dynamicSizesVector,
            hasDynamicRank);
    }

    /// <summary>
    /// シンボリック次元を表す<see cref="EDim"/>を作成します（整数値）。
    /// </summary>
    /// <param name="builder">FlatBufferBuilder</param>
    /// <param name="value">次元の値</param>
    /// <returns>構成された<see cref="EDim"/>のオフセット</returns>
    public static Offset<EDim> CreateEDim(FlatBufferBuilder builder, int value)
    {
        var intOffset = Int.CreateInt(builder, value);
        return EDim.CreateEDim(builder, SymbolicDim.Int, intOffset.Value);
    }

    /// <summary>
    /// シンボリック次元を表す<see cref="EDim"/>を作成します（バイト参照）。
    /// </summary>
    /// <param name="builder">FlatBufferBuilder</param>
    /// <param name="symbolicIndex">シンボリック次元名へのインデックス</param>
    /// <returns>構成された<see cref="EDim"/>のオフセット</returns>
    public static Offset<EDim> CreateSymbolicEDim(FlatBufferBuilder builder, byte symbolicIndex)
    {
        var byteOffset = Byte.CreateByte(builder, symbolicIndex);
        return EDim.CreateEDim(builder, SymbolicDim.Byte, byteOffset.Value);
    }

    #endregion

    #region Buffer / DataSegment

    /// <summary>
    /// データバッファを表す<see cref="SentisFlatBuffer.Buffer"/>を作成します。
    /// </summary>
    /// <param name="builder">FlatBufferBuilder</param>
    /// <param name="data">バイトデータ</param>
    /// <returns>構成された<see cref="SentisFlatBuffer.Buffer"/>のオフセット</returns>
    /// <example>
    /// <code>
    /// var weightData = new byte[] { ... };
    /// var buffer = SentisUtility.CreateBuffer(builder, weightData);
    /// </code>
    /// </example>
    private static Offset<Buffer> CreateBuffer(FlatBufferBuilder builder, byte[] data)
    {
        var storageVector = Buffer.CreateStorageVector(builder, data);
        return Buffer.CreateBuffer(builder, storageVector);
    }

    /// <summary>
    /// float配列からデータバッファを作成します。
    /// </summary>
    /// <param name="builder">FlatBufferBuilder</param>
    /// <param name="data">float配列</param>
    /// <returns>構成された<see cref="SentisFlatBuffer.Buffer"/>のオフセット</returns>
    /// <example>
    /// <code>
    /// var weights = new float[] { 0.1f, 0.2f, 0.3f };
    /// var buffer = SentisUtility.CreateBuffer(builder, weights);
    /// </code>
    /// </example>
    public static Offset<Buffer> CreateBuffer(FlatBufferBuilder builder, float[] data)
    {
        var bytes = MemoryMarshal.AsBytes(data.AsSpan()).ToArray();
        return CreateBuffer(builder, bytes);
    }

    /// <summary>
    /// 2次元配列からデータバッファを作成します。
    /// </summary>
    /// <param name="builder">FlatBufferBuilder</param>
    /// <param name="data">2次元float配列</param>
    /// <returns>構成された<see cref="SentisFlatBuffer.Buffer"/>のオフセット</returns>
    public static Offset<Buffer> CreateBuffer(FlatBufferBuilder builder, float[,] data)
    {
        var bytes = MemoryMarshal.AsBytes(FlattenArray(data)).ToArray();
        return CreateBuffer(builder, bytes);
    }

    /// <summary>
    /// 3次元配列からデータバッファを作成します。
    /// </summary>
    /// <param name="builder">FlatBufferBuilder</param>
    /// <param name="data">3次元float配列</param>
    /// <returns>構成された<see cref="SentisFlatBuffer.Buffer"/>のオフセット</returns>
    public static Offset<Buffer> CreateBuffer(FlatBufferBuilder builder, float[,,] data)
    {
        var bytes = MemoryMarshal.AsBytes(FlattenArray(data)).ToArray();
        return CreateBuffer(builder, bytes);
    }

    /// <summary>
    /// 4次元配列からデータバッファを作成します。
    /// </summary>
    /// <param name="builder">FlatBufferBuilder</param>
    /// <param name="data">4次元float配列</param>
    /// <returns>構成された<see cref="SentisFlatBuffer.Buffer"/>のオフセット</returns>
    public static Offset<Buffer> CreateBuffer(FlatBufferBuilder builder, float[,,,] data)
    {
        var bytes = MemoryMarshal.AsBytes(FlattenArray(data)).ToArray();
        return CreateBuffer(builder, bytes);
    }

    /// <summary>
    /// データセグメントを表す<see cref="DataSegment"/>を作成します。
    /// </summary>
    /// <param name="builder">FlatBufferBuilder</param>
    /// <param name="offset">データのオフセット（バイト位置）</param>
    /// <param name="size">データのサイズ（バイト）</param>
    /// <returns>構成された<see cref="DataSegment"/>のオフセット</returns>
    public static Offset<DataSegment> CreateDataSegment(FlatBufferBuilder builder, ulong offset, ulong size)
    {
        return DataSegment.CreateDataSegment(builder, offset, size);
    }

    #endregion

    #region Utility Methods

    /// <summary>
    /// Programをバイト配列としてシリアライズします。
    /// </summary>
    /// <param name="builder">FlatBufferBuilder</param>
    /// <param name="programOffset">Programのオフセット</param>
    /// <returns>シリアライズされたバイト配列</returns>
    public static byte[] FinishAndGetBytes(FlatBufferBuilder builder, Offset<Program> programOffset)
    {
        Program.FinishProgramBuffer(builder, programOffset);
        return builder.SizedByteArray();
    }

    /// <summary>
    /// テンソルのバイト長を計算します。
    /// </summary>
    /// <param name="shape">テンソルの形状</param>
    /// <param name="scalarType">スカラー型</param>
    /// <returns>バイト長</returns>
    private static int CalculateTensorLengthByte(int[] shape, ScalarType scalarType)
    {
        if (shape.Length == 0) return 0;

        var elementCount = 1;
        foreach (var dim in shape)
        {
            if (dim <= 0) return 0; // 動的次元の場合は0を返す
            elementCount *= dim;
        }

        var bytesPerElement = scalarType switch
        {
            ScalarType.FLOAT => 4,
            ScalarType.INT => 4,
            ScalarType.BYTE => 1,
            ScalarType.SHORT => 2,
            _ => 4
        };

        return elementCount * bytesPerElement;
    }

    #endregion

    #region Array Flatten Helpers

    /// <summary>
    /// 2次元配列を1次元のSpanとしてフラット化します。
    /// </summary>
    /// <param name="data">フラット化する2次元配列</param>
    /// <returns>フラット化されたSpan</returns>
    private static Span<float> FlattenArray(float[,] data)
    {
        return MemoryMarshal.CreateSpan(ref data[0, 0], data.Length);
    }

    /// <summary>
    /// 3次元配列を1次元のSpanとしてフラット化します。
    /// </summary>
    /// <param name="data">フラット化する3次元配列</param>
    /// <returns>フラット化されたSpan</returns>
    private static Span<float> FlattenArray(float[,,] data)
    {
        return MemoryMarshal.CreateSpan(ref data[0, 0, 0], data.Length);
    }

    /// <summary>
    /// 4次元配列を1次元のSpanとしてフラット化します。
    /// </summary>
    /// <param name="data">フラット化する4次元配列</param>
    /// <returns>フラット化されたSpan</returns>
    private static Span<float> FlattenArray(float[,,,] data)
    {
        return MemoryMarshal.CreateSpan(ref data[0, 0, 0, 0], data.Length);
    }

    #endregion
}