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

using static TorchSharp.torch;
using static TorchSharp.torch.nn;

namespace Foxtamp.MLCoreModule.PositionalEncodings;

/// <summary>
/// Rotary Position Embedding (RoPE) の実装。
/// Query/Keyテンソルに回転変換を適用して位置情報をエンコードします。
/// </summary>
/// <remarks>
/// 参考論文: "RoFormer: Enhanced Transformer with Rotary Position Embedding"
/// https://arxiv.org/abs/2104.09864
/// </remarks>
public sealed class RotaryPositionalEncoding : Module, IPositionalEncoding
{
    private readonly int _maxSequenceLength;
    private readonly Tensor _cosCache;
    private readonly Tensor _sinCache;

    /// <inheritdoc/>
    public PositionalEncodingType EncodingType => PositionalEncodingType.QueryKeyTransform;

    /// <summary>
    /// RoPEインスタンスを初期化します。
    /// </summary>
    /// <param name="dimension">埋め込み次元数（偶数である必要があります）</param>
    /// <param name="maxSequenceLength">サポートする最大シーケンス長</param>
    /// <param name="baseFrequency">基底周波数（デフォルト: 10000.0）</param>
    /// <param name="device">テンソルを配置するデバイス（デフォルト: CPU）</param>
    /// <param name="dtype">テンソルのデータ型（デフォルト: float32）</param>
    /// <exception cref="ArgumentException">dimensionが偶数でない場合</exception>
    public RotaryPositionalEncoding(
        int dimension,
        int maxSequenceLength = 2048,
        float baseFrequency = 10000.0f,
        Device? device = null,
        ScalarType dtype = ScalarType.Float32)
        : base(nameof(RotaryPositionalEncoding))
    {
        if (dimension % 2 != 0)
        {
            throw new ArgumentException("次元数は偶数である必要があります。", nameof(dimension));
        }

        _maxSequenceLength = maxSequenceLength;
        var targetDevice = device ?? CPU;

        // 事前に最大長までのsin/cosキャッシュを生成
        var halfDim = dimension / 2;

        // 逆周波数の計算: 1 / (base^(2i/d)) for i in [0, d/2)
        using var invFreqIndices = arange(0, halfDim, dtype: ScalarType.Float32, device: targetDevice);
        using var invFreq = 1.0f / pow(baseFrequency, invFreqIndices * 2.0f / dimension);

        // 位置インデックス
        using var positions = arange(0, maxSequenceLength, dtype: ScalarType.Float32, device: targetDevice);

        // 位置 × 逆周波数 [maxSequenceLength, halfDim]
        using var freqs = outer(positions, invFreq);

        // キャッシュを作成
        _cosCache = cos(freqs).to(dtype);
        _sinCache = sin(freqs).to(dtype);

        // バッファとして登録（persistent=trueで永続化）
        register_buffer("rope_cos", _cosCache, persistent: true);
        register_buffer("rope_sin", _sinCache, persistent: true);
    }

    /// <inheritdoc/>
    /// <exception cref="ArgumentOutOfRangeException">最大長を超えた位置が入力されています。</exception>
    public (Tensor query, Tensor key) ApplyToQueryKey(Tensor query, Tensor key, int positionOffset = 0)
    {
        var seqLen = (int)query.shape[^2];
        var device = query.device;
        var dtype = query.dtype;

        if (positionOffset + seqLen > _maxSequenceLength)
        {
            throw new ArgumentOutOfRangeException($"要求されたシーケンス範囲（offset: {positionOffset}, length: {seqLen}）が最大長（{_maxSequenceLength}）を超えています。");
        }

        // 事前生成したキャッシュから必要な範囲をスライス
        var cos = _cosCache[TensorIndex.Slice(positionOffset, positionOffset + seqLen)];
        var sin = _sinCache[TensorIndex.Slice(positionOffset, positionOffset + seqLen)];

        // デバイスまたはデータ型が異なる場合は変換
        if (cos.device != device || cos.dtype != dtype)
        {
            var newCos = cos.to(dtype, device);
            var newSin = sin.to(dtype, device);
            cos.Dispose();
            sin.Dispose();
            cos = newCos;
            sin = newSin;
        }

        var rotatedQuery = ApplyRotaryEmbedding(query, cos, sin);
        var rotatedKey = ApplyRotaryEmbedding(key, cos, sin);

        return (rotatedQuery, rotatedKey);
    }

    /// <inheritdoc/>
    /// <exception cref="NotSupportedException">RoPEはQueryKeyTransformタイプのため、GetScoreBiasはサポートされていません。</exception>
    public Tensor GetScoreBias(int queryLength, int keyLength, Device device, ScalarType dtype)
    {
        throw new NotSupportedException("RoPEはQueryKeyTransformタイプのため、GetScoreBiasはサポートされていません。");
    }

    /// <summary>
    /// 回転埋め込みをテンソルに適用します。
    /// </summary>
    /// <param name="x">入力テンソル [batch, seqLen, dim]</param>
    /// <param name="cos">コサインキャッシュ [seqLen, dim/2]</param>
    /// <param name="sin">サインキャッシュ [seqLen, dim/2]</param>
    /// <returns>回転適用後のテンソル</returns>
    private static Tensor ApplyRotaryEmbedding(Tensor x, Tensor cos, Tensor sin)
    {
        var halfDim = x.shape[^1] / 2;

        using var x1 = x.Dimensions == 3 ? x[TensorIndex.Colon, TensorIndex.Colon, TensorIndex.Slice(0, halfDim)] : x[TensorIndex.Colon, TensorIndex.Slice(0, halfDim)];
        using var x2 = x.Dimensions == 3 ? x[TensorIndex.Colon, TensorIndex.Colon, TensorIndex.Slice(halfDim)] : x[TensorIndex.Colon, TensorIndex.Slice(halfDim)];

        // 回転変換: [x1, x2] -> [x1*cos - x2*sin, x1*sin + x2*cos]
        using var rotatedX1 = x1 * cos - x2 * sin;
        using var rotatedX2 = x1 * sin + x2 * cos;
        return cat([rotatedX1, rotatedX2], dim: -1);
    }
}