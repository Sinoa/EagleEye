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
    private readonly int _dimension;
    private readonly float _baseFrequency;
    private Tensor? _cosCache;
    private Tensor? _sinCache;

    /// <inheritdoc/>
    public PositionalEncodingType EncodingType => PositionalEncodingType.QueryKeyTransform;

    /// <summary>
    /// RoPEインスタンスを初期化します。
    /// </summary>
    /// <param name="dimension">埋め込み次元数（偶数である必要があります）</param>
    /// <param name="maxSequenceLength">サポートする最大シーケンス長</param>
    /// <param name="baseFrequency">基底周波数（デフォルト: 10000.0）</param>
    /// <exception cref="ArgumentException">dimensionが偶数でない場合</exception>
    public RotaryPositionalEncoding(int dimension, int maxSequenceLength = 2048, float baseFrequency = 10000.0f)
        : base(nameof(RotaryPositionalEncoding))
    {
        if (dimension % 2 != 0)
        {
            throw new ArgumentException("次元数は偶数である必要があります。", nameof(dimension));
        }

        _dimension = dimension;
        _baseFrequency = baseFrequency;
    }

    /// <inheritdoc/>
    public (Tensor query, Tensor key) ApplyToQueryKey(Tensor query, Tensor key, int positionOffset = 0)
    {
        var seqLen = (int)query.shape[1];
        var device = query.device;
        var dtype = query.dtype;

        EnsureCacheBuilt(seqLen + positionOffset, device, dtype);

        var cos = _cosCache![TensorIndex.Slice(positionOffset, positionOffset + seqLen)];
        var sin = _sinCache![TensorIndex.Slice(positionOffset, positionOffset + seqLen)];

        var rotatedQuery = ApplyRotaryEmbedding(query, cos, sin);
        var rotatedKey = ApplyRotaryEmbedding(key, cos, sin);

        return (rotatedQuery, rotatedKey);
    }

    /// <inheritdoc/>
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
        var halfDim = x.shape[2] / 2;

        var x1 = x[TensorIndex.Colon, TensorIndex.Colon, TensorIndex.Slice(0, halfDim)];
        var x2 = x[TensorIndex.Colon, TensorIndex.Colon, TensorIndex.Slice(halfDim)];

        // 回転変換: [x1, x2] -> [x1*cos - x2*sin, x1*sin + x2*cos]
        var rotatedX1 = x1 * cos - x2 * sin;
        var rotatedX2 = x1 * sin + x2 * cos;

        return cat([rotatedX1, rotatedX2], dim: -1);
    }

    /// <summary>
    /// sin/cosキャッシュを構築します。
    /// </summary>
    /// <param name="seqLen">必要なシーケンス長</param>
    /// <param name="device">デバイス</param>
    /// <param name="dtype">データ型</param>
    private void EnsureCacheBuilt(int seqLen, Device device, ScalarType dtype)
    {
        if (_cosCache is not null && _sinCache is not null && _cosCache.shape[0] >= seqLen && _cosCache.device == device && _cosCache.dtype == dtype)
        {
            return;
        }

        var halfDim = _dimension / 2;

        // 逆周波数の計算: 1 / (base^(2i/d)) for i in [0, d/2)
        using var invFreqIndices = arange(0, halfDim, dtype: ScalarType.Float32, device: device);
        var invFreq = 1.0f / pow(_baseFrequency, invFreqIndices * 2.0f / _dimension);

        // 位置インデックス
        using var positions = arange(0, seqLen, dtype: ScalarType.Float32, device: device);

        // 位置 × 逆周波数 [seqLen, halfDim]
        var freqs = outer(positions, invFreq);

        // キャッシュを作成
        _cosCache?.Dispose();
        _sinCache?.Dispose();

        _cosCache = cos(freqs).to(dtype);
        _sinCache = sin(freqs).to(dtype);

        // バッファとして登録（persistent=trueで永続化）
        register_buffer("cos", _cosCache, persistent: true);
        register_buffer("sin", _sinCache, persistent: true);
        RegisterComponents();
    }

    /// <summary>
    /// キャッシュを解放します。
    /// </summary>
    public void ClearCache()
    {
        _cosCache?.Dispose();
        _sinCache?.Dispose();
        _cosCache = null;
        _sinCache = null;
    }
}