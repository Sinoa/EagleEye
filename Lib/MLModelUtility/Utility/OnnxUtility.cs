// zlib License
// 
// Copyright (c) 2026 Sinoa
// 
// This software is provided ‘as-is’, without any express or implied
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
using Onnx;

namespace MLModelUtility.Utility;

/// <summary>
/// ONNXモデルの構築に必要なProtocol Bufferオブジェクトを生成するユーティリティクラスです。
/// </summary>
/// <remarks>
/// <para>このクラスは以下の機能を提供します：</para>
/// <list type="bullet">
///   <item><description><see cref="ValueInfoProto"/> - 入出力テンソルの型情報</description></item>
///   <item><description><see cref="AttributeProto"/> - ノード属性（演算子のパラメータ）</description></item>
///   <item><description><see cref="TensorProto"/> - 定数テンソル（重み、バイアス等）</description></item>
/// </list>
/// </remarks>
public static class OnnxUtility
{
    #region ModelProto / GraphProto / NodeProto

    /// <summary>
    /// ONNXモデルを表す<see cref="ModelProto"/>を作成します。
    /// </summary>
    /// <param name="producerName">モデルを生成したツールやライブラリの名前</param>
    /// <param name="producerVersion">プロデューサーのバージョン文字列</param>
    /// <param name="graph">モデルの計算グラフ</param>
    /// <param name="irVersion">ONNX IRバージョン（デフォルト: 9）</param>
    /// <param name="opsetVersion">使用するOperator Setのバージョン（デフォルト: 24）</param>
    /// <returns>構成された<see cref="ModelProto"/>インスタンス</returns>
    /// <example>
    /// <code>
    /// var graph = OnnxUtility.CreateGraph("main", input, output, initializers, nodes);
    /// var model = OnnxUtility.CreateModel("MyTool", "1.0.0", graph);
    /// </code>
    /// </example>
    public static ModelProto CreateModel(string producerName, string producerVersion, GraphProto graph, long irVersion = 9, long opsetVersion = 24)
    {
        return new ModelProto
        {
            IrVersion = irVersion,
            ProducerName = producerName,
            ProducerVersion = producerVersion,
            OpsetImport =
            {
                new OperatorSetIdProto
                {
                    Domain = "",
                    Version = opsetVersion,
                },
            },
            Graph = graph,
        };
    }

    /// <summary>
    /// ONNXモデルの計算グラフを表す<see cref="GraphProto"/>を作成します。
    /// </summary>
    /// <param name="name">グラフの名前</param>
    /// <param name="input">グラフへの入力テンソル情報</param>
    /// <param name="output">グラフからの出力テンソル情報</param>
    /// <param name="initializers">グラフで使用する初期化済みテンソル（重み、バイアス等）</param>
    /// <param name="nodes">グラフ内の演算ノード</param>
    /// <returns>構成された<see cref="GraphProto"/>インスタンス</returns>
    /// <example>
    /// <code>
    /// var input = OnnxUtility.CreateValueInfo("input", new long[] { -1, 10 });
    /// var output = OnnxUtility.CreateValueInfo("output", new long[] { -1, 5 });
    /// var graph = OnnxUtility.CreateGraph("main", input, output, initializers, nodes);
    /// </code>
    /// </example>
    public static GraphProto CreateGraph(string name, ValueInfoProto input, ValueInfoProto output, TensorProto[]? initializers = null, NodeProto[]? nodes = null)
    {
        var graph = new GraphProto
        {
            Name = name,
        };

        graph.Input.Add(input);

        if (initializers != null)
        {
            graph.Initializer.AddRange(initializers);
        }

        if (nodes != null)
        {
            graph.Node.AddRange(nodes);
        }

        graph.Output.Add(output);

        return graph;
    }

    /// <summary>
    /// ONNXグラフ内の演算ノードを表す<see cref="NodeProto"/>を作成します。
    /// </summary>
    /// <param name="name">ノードの名前（グラフ内で一意である必要があります）</param>
    /// <param name="opType">演算子の種類（例: "MatMul", "Add", "Relu", "Softmax"等）</param>
    /// <param name="inputNames">入力テンソルの名前の配列</param>
    /// <param name="outputNames">出力テンソルの名前の配列</param>
    /// <param name="attributes">演算子に渡す属性パラメータ</param>
    /// <returns>構成された<see cref="NodeProto"/>インスタンス</returns>
    /// <example>
    /// <code>
    /// // MatMulノードの作成
    /// var matmul = OnnxUtility.CreateNode("matmul_0", "MatMul", 
    ///     new[] { "input", "weight" }, new[] { "matmul_out" });
    /// 
    /// // 属性付きノードの作成
    /// var softmax = OnnxUtility.CreateNode("softmax_0", "Softmax",
    ///     new[] { "input" }, new[] { "output" },
    ///     new[] { OnnxUtility.CreateAttribute("axis", 1L) });
    /// </code>
    /// </example>
    public static NodeProto CreateNode(string name, string opType, string[] inputNames, string[] outputNames, AttributeProto[]? attributes = null)
    {
        var node = new NodeProto
        {
            Name = name,
            OpType = opType,
        };

        node.Input.AddRange(inputNames);
        node.Output.AddRange(outputNames);

        if (attributes != null)
        {
            node.Attribute.AddRange(attributes);
        }

        return node;
    }

    #endregion

    #region ValueInfoProto

    /// <summary>
    /// テンソルの型情報を表す<see cref="ValueInfoProto"/>を作成します。
    /// </summary>
    /// <param name="name">テンソルの名前（グラフ内で一意である必要があります）</param>
    /// <param name="shape">
    /// テンソルの形状を表す配列。負の値は動的次元（バッチサイズ等）を示します。
    /// <example>例: <c>new long[] { -1, 3, 224, 224 }</c> は可変バッチの3チャネル224x224画像</example>
    /// </param>
    /// <param name="dataType">データ型（デフォルト: Float）</param>
    /// <returns>構成された<see cref="ValueInfoProto"/>インスタンス</returns>
    /// <example>
    /// <code>
    /// // 可変バッチサイズの入力テンソル（バッチ x 10次元）
    /// var input = OnnxUtility.CreateValueInfo("input", new long[] { -1, 10 });
    /// 
    /// // 固定サイズの出力テンソル（1 x 5次元）
    /// var output = OnnxUtility.CreateValueInfo("output", new long[] { 1, 5 });
    /// </code>
    /// </example>
    public static ValueInfoProto CreateValueInfo(string name,　long[] shape,　TensorProto.Types.DataType dataType = TensorProto.Types.DataType.Float)
    {
        var shapeProto = new TensorShapeProto();

        foreach (var dim in shape)
        {
            var dimension = new TensorShapeProto.Types.Dimension();

            if (dim < 0)
            {
                dimension.DimParam = "batch";
            }
            else
            {
                dimension.DimValue = dim;
            }

            shapeProto.Dim.Add(dimension);
        }

        return new ValueInfoProto
        {
            Name = name,
            Type = new TypeProto
            {
                TensorType = new TypeProto.Types.Tensor
                {
                    ElemType = (int)dataType,
                    Shape = shapeProto,
                },
            },
        };
    }

    #endregion

    #region AttributeProto - Scalar Values

    /// <summary>
    /// float型スカラー属性を作成します。
    /// </summary>
    /// <param name="name">属性名</param>
    /// <param name="value">属性値</param>
    /// <returns>構成された<see cref="AttributeProto"/>インスタンス</returns>
    /// <example>
    /// <code>
    /// // Dropout層のratio属性
    /// var ratio = OnnxUtility.CreateAttribute("ratio", 0.5f);
    /// </code>
    /// </example>
    public static AttributeProto CreateAttribute(string name, float value)
    {
        return new AttributeProto
        {
            Name = name,
            Type = AttributeProto.Types.AttributeType.Float,
            F = value,
        };
    }

    /// <summary>
    /// 整数型スカラー属性を作成します。
    /// </summary>
    /// <param name="name">属性名</param>
    /// <param name="value">属性値</param>
    /// <returns>構成された<see cref="AttributeProto"/>インスタンス</returns>
    /// <example>
    /// <code>
    /// // Softmax層のaxis属性
    /// var axis = OnnxUtility.CreateAttribute("axis", 1L);
    /// </code>
    /// </example>
    public static AttributeProto CreateAttribute(string name, long value)
    {
        return new AttributeProto
        {
            Name = name,
            Type = AttributeProto.Types.AttributeType.Int,
            I = value,
        };
    }

    /// <summary>
    /// 文字列型スカラー属性を作成します。
    /// </summary>
    /// <param name="name">属性名</param>
    /// <param name="value">属性値</param>
    /// <returns>構成された<see cref="AttributeProto"/>インスタンス</returns>
    /// <example>
    /// <code>
    /// // 活性化関数の種類を指定
    /// var activationType = OnnxUtility.CreateAttribute("activation", "relu");
    /// </code>
    /// </example>
    public static AttributeProto CreateAttribute(string name, string value)
    {
        return new AttributeProto
        {
            Name = name,
            Type = AttributeProto.Types.AttributeType.String,
            S = ByteString.CopyFromUtf8(value),
        };
    }

    /// <summary>
    /// テンソル型属性を作成します。
    /// </summary>
    /// <param name="name">属性名</param>
    /// <param name="value">テンソル値</param>
    /// <returns>構成された<see cref="AttributeProto"/>インスタンス</returns>
    /// <example>
    /// <code>
    /// // Constantノードのvalue属性
    /// var tensor = OnnxUtility.CreateTensor("const_value", new float[] { 1.0f, 2.0f });
    /// var valueAttr = OnnxUtility.CreateAttribute("value", tensor);
    /// </code>
    /// </example>
    public static AttributeProto CreateAttribute(string name, TensorProto value)
    {
        return new AttributeProto
        {
            Name = name,
            Type = AttributeProto.Types.AttributeType.Tensor,
            T = value,
        };
    }

    #endregion

    #region AttributeProto - Array Values

    /// <summary>
    /// float型配列属性を作成します。
    /// </summary>
    /// <param name="name">属性名</param>
    /// <param name="values">属性値の配列</param>
    /// <returns>構成された<see cref="AttributeProto"/>インスタンス</returns>
    /// <example>
    /// <code>
    /// // パディング値の配列
    /// var pads = OnnxUtility.CreateAttribute("pads", new float[] { 1.0f, 1.0f, 1.0f, 1.0f });
    /// </code>
    /// </example>
    public static AttributeProto CreateAttribute(string name, float[] values)
    {
        var attribute = new AttributeProto
        {
            Name = name,
            Type = AttributeProto.Types.AttributeType.Floats,
        };

        attribute.Floats.AddRange(values);
        return attribute;
    }

    /// <summary>
    /// 整数型配列属性を作成します。
    /// </summary>
    /// <param name="name">属性名</param>
    /// <param name="values">属性値の配列</param>
    /// <returns>構成された<see cref="AttributeProto"/>インスタンス</returns>
    /// <example>
    /// <code>
    /// // カーネルサイズの配列
    /// var kernelShape = OnnxUtility.CreateAttribute("kernel_shape", new long[] { 3, 3 });
    /// </code>
    /// </example>
    public static AttributeProto CreateAttribute(string name, long[] values)
    {
        var attribute = new AttributeProto
        {
            Name = name,
            Type = AttributeProto.Types.AttributeType.Ints,
        };

        attribute.Ints.AddRange(values);
        return attribute;
    }

    /// <summary>
    /// 文字列型配列属性を作成します。
    /// </summary>
    /// <param name="name">属性名</param>
    /// <param name="values">属性値の配列</param>
    /// <returns>構成された<see cref="AttributeProto"/>インスタンス</returns>
    /// <example>
    /// <code>
    /// // 複数のラベル名を指定
    /// var labels = OnnxUtility.CreateAttribute("labels", new string[] { "cat", "dog", "bird" });
    /// </code>
    /// </example>
    public static AttributeProto CreateAttribute(string name, string[] values)
    {
        var attribute = new AttributeProto
        {
            Name = name,
            Type = AttributeProto.Types.AttributeType.Strings,
        };

        attribute.Strings.AddRange(values.Select(x => ByteString.CopyFromUtf8(x)));
        return attribute;
    }

    /// <summary>
    /// テンソル型配列属性を作成します。
    /// </summary>
    /// <param name="name">属性名</param>
    /// <param name="values">テンソル値の配列</param>
    /// <returns>構成された<see cref="AttributeProto"/>インスタンス</returns>
    /// <example>
    /// <code>
    /// // 複数のテンソルを属性として設定
    /// var tensors = new[]
    /// {
    ///     OnnxUtility.CreateTensor("t1", new float[] { 1.0f }),
    ///     OnnxUtility.CreateTensor("t2", new float[] { 2.0f }),
    /// };
    /// var attr = OnnxUtility.CreateAttribute("tensors", tensors);
    /// </code>
    /// </example>
    public static AttributeProto CreateAttribute(string name, TensorProto[] values)
    {
        var attribute = new AttributeProto
        {
            Name = name,
            Type = AttributeProto.Types.AttributeType.Tensors,
        };

        attribute.Tensors.AddRange(values);
        return attribute;
    }

    #endregion

    #region TensorProto - Public Factory Methods

    /// <summary>
    /// スカラー値から<see cref="TensorProto"/>を作成します。
    /// </summary>
    /// <remarks>
    /// スカラー値は<see cref="TensorProto.FloatData"/>に格納されます（0次元テンソル）。
    /// </remarks>
    /// <param name="name">テンソル名</param>
    /// <param name="value">スカラー値</param>
    /// <returns>構成された<see cref="TensorProto"/>インスタンス</returns>
    /// <example>
    /// <code>
    /// // スカラー定数の作成
    /// var epsilon = OnnxUtility.CreateTensor("epsilon", 1e-5f);
    /// </code>
    /// </example>
    public static TensorProto CreateTensor(string name, float value)
    {
        var tensorProto = CreateBaseTensorProto(name);
        tensorProto.FloatData.Add(value);
        return tensorProto;
    }

    /// <summary>
    /// 1次元配列から<see cref="TensorProto"/>を作成します。
    /// </summary>
    /// <param name="name">テンソル名</param>
    /// <param name="data">1次元float配列</param>
    /// <returns>構成された<see cref="TensorProto"/>インスタンス</returns>
    /// <example>
    /// <code>
    /// var bias = OnnxUtility.CreateTensor("bias", new float[] { 0.1f, 0.2f, 0.3f });
    /// </code>
    /// </example>
    public static TensorProto CreateTensor(string name, float[] data)
        => CreateTensorProtoCore(name, data.AsSpan(), data.Length);

    /// <summary>
    /// 2次元配列から<see cref="TensorProto"/>を作成します。
    /// </summary>
    /// <param name="name">テンソル名</param>
    /// <param name="data">2次元float配列 [rows, cols]</param>
    /// <returns>構成された<see cref="TensorProto"/>インスタンス</returns>
    /// <example>
    /// <code>
    /// var weight = OnnxUtility.CreateTensor("weight", new float[,] { { 1, 2 }, { 3, 4 } });
    /// </code>
    /// </example>
    public static TensorProto CreateTensor(string name, float[,] data)
    {
        var dim0 = data.GetLength(0);
        var dim1 = data.GetLength(1);
        return CreateTensorProtoCore(name, FlattenArray(data, dim0, dim1).AsSpan(), dim0, dim1);
    }

    /// <summary>
    /// 3次元配列から<see cref="TensorProto"/>を作成します。
    /// </summary>
    /// <param name="name">テンソル名</param>
    /// <param name="data">3次元float配列 [depth, rows, cols]</param>
    /// <returns>構成された<see cref="TensorProto"/>インスタンス</returns>
    /// <example>
    /// <code>
    /// // 3次元テンソル（2x3x4）を作成
    /// var tensor3d = OnnxUtility.CreateTensor("tensor3d", new float[2, 3, 4]);
    /// </code>
    /// </example>
    public static TensorProto CreateTensor(string name, float[,,] data)
    {
        var dim0 = data.GetLength(0);
        var dim1 = data.GetLength(1);
        var dim2 = data.GetLength(2);
        return CreateTensorProtoCore(name, FlattenArray(data, dim0, dim1, dim2).AsSpan(), dim0, dim1, dim2);
    }

    /// <summary>
    /// 4次元配列から<see cref="TensorProto"/>を作成します。
    /// </summary>
    /// <remarks>
    /// 主にCNN畳み込み層の重み [out_channels, in_channels, height, width] に使用されます。
    /// </remarks>
    /// <param name="name">テンソル名</param>
    /// <param name="data">4次元float配列</param>
    /// <returns>構成された<see cref="TensorProto"/>インスタンス</returns>
    /// <example>
    /// <code>
    /// // CNN畳み込みカーネル（32出力チャネル, 3入力チャネル, 3x3カーネル）
    /// var kernel = OnnxUtility.CreateTensor("conv_weight", new float[32, 3, 3, 3]);
    /// </code>
    /// </example>
    public static TensorProto CreateTensor(string name, float[,,,] data)
    {
        var dim0 = data.GetLength(0);
        var dim1 = data.GetLength(1);
        var dim2 = data.GetLength(2);
        var dim3 = data.GetLength(3);
        return CreateTensorProtoCore(name, FlattenArray(data, dim0, dim1, dim2, dim3).AsSpan(), dim0, dim1, dim2, dim3);
    }

    /// <summary>
    /// 1次元配列と任意の次元情報から<see cref="TensorProto"/>を作成します。
    /// </summary>
    /// <remarks>
    /// 既にフラット化されたデータと形状情報を別々に持つ場合に使用します。
    /// データの総要素数と dims の積が一致する必要があります。
    /// </remarks>
    /// <param name="name">テンソル名</param>
    /// <param name="data">フラット化されたfloat配列</param>
    /// <param name="dims">各次元のサイズ</param>
    /// <returns>構成された<see cref="TensorProto"/>インスタンス</returns>
    /// <example>
    /// <code>
    /// // 2x3 行列を作成
    /// var tensor = OnnxUtility.CreateTensor("matrix", new float[] { 1, 2, 3, 4, 5, 6 }, 2, 3);
    /// </code>
    /// </example>
    public static TensorProto CreateTensor(string name, float[] data, params long[] dims)
        => CreateTensorProtoCore(name, data.AsSpan(), dims);

    #endregion

    #region TensorProto - Private Implementation

    /// <summary>
    /// Float型TensorProtoの基本インスタンスを作成します。
    /// </summary>
    /// <param name="name">テンソル名</param>
    /// <returns>名前とデータ型が設定された<see cref="TensorProto"/>インスタンス</returns>
    private static TensorProto CreateBaseTensorProto(string name)
    {
        return new TensorProto
        {
            Name = name,
            DataType = (int)TensorProto.Types.DataType.Float,
        };
    }

    /// <summary>
    /// TensorProto作成のコア実装。次元情報の設定とRawDataへのバイナリ格納を行います。
    /// </summary>
    /// <param name="name">テンソル名</param>
    /// <param name="data">データのSpan</param>
    /// <param name="dims">各次元のサイズ</param>
    /// <returns>構成された<see cref="TensorProto"/>インスタンス</returns>
    private static TensorProto CreateTensorProtoCore(string name, ReadOnlySpan<float> data, params long[] dims)
    {
        var tensorProto = CreateBaseTensorProto(name);

        foreach (var dim in dims)
        {
            tensorProto.Dims.Add(dim);
        }

        tensorProto.RawData = ByteString.CopyFrom(MemoryMarshal.AsBytes(data));
        return tensorProto;
    }

    #endregion

    #region Array Flatten Helpers

    /// <summary>
    /// 2次元配列を1次元配列にフラット化します。
    /// </summary>
    /// <param name="data">フラット化する2次元配列</param>
    /// <param name="dim0">第1次元のサイズ</param>
    /// <param name="dim1">第2次元のサイズ</param>
    /// <returns>フラット化された1次元配列</returns>
    private static float[] FlattenArray(float[,] data, int dim0, int dim1)
    {
        var buffer = new float[dim0 * dim1];
        Buffer.BlockCopy(data, 0, buffer, 0, buffer.Length * sizeof(float));
        return buffer;
    }

    /// <summary>
    /// 3次元配列を1次元配列にフラット化します。
    /// </summary>
    /// <param name="data">フラット化する3次元配列</param>
    /// <param name="dim0">第1次元のサイズ</param>
    /// <param name="dim1">第2次元のサイズ</param>
    /// <param name="dim2">第3次元のサイズ</param>
    /// <returns>フラット化された1次元配列</returns>
    private static float[] FlattenArray(float[,,] data, int dim0, int dim1, int dim2)
    {
        var buffer = new float[dim0 * dim1 * dim2];
        Buffer.BlockCopy(data, 0, buffer, 0, buffer.Length * sizeof(float));
        return buffer;
    }

    /// <summary>
    /// 4次元配列を1次元配列にフラット化します。
    /// </summary>
    /// <param name="data">フラット化する4次元配列</param>
    /// <param name="dim0">第1次元のサイズ</param>
    /// <param name="dim1">第2次元のサイズ</param>
    /// <param name="dim2">第3次元のサイズ</param>
    /// <param name="dim3">第4次元のサイズ</param>
    /// <returns>フラット化された1次元配列</returns>
    private static float[] FlattenArray(float[,,,] data, int dim0, int dim1, int dim2, int dim3)
    {
        var buffer = new float[dim0 * dim1 * dim2 * dim3];
        Buffer.BlockCopy(data, 0, buffer, 0, buffer.Length * sizeof(float));
        return buffer;
    }

    #endregion
}