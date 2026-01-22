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

using TorchSharp;
using static TorchSharp.torch;

namespace Foxtamp.MLCoreModule.PositionalEncodings;

/// <summary>
/// Attention with Linear Biases (ALiBi) の実装。
/// アテンションスコアに線形バイアスを加算して位置情報をエンコードします。
/// </summary>
/// <remarks>
/// 参考論文: "Train Short, Test Long: Attention with Linear Biases Enables Input Length Extrapolation"
/// https://arxiv.org/abs/2108.12409
/// </remarks>
public sealed class ALiBiPositionalEncoding : IPositionalEncoding
{
    private readonly float _slope;
    private Tensor? _biasCache;
    private int _cachedQueryLength;
    private int _cachedKeyLength;

    /// <inheritdoc/>
    public PositionalEncodingType EncodingType => PositionalEncodingType.ScoreBias;

    /// <summary>
    /// ALiBiインスタンスを初期化します。
    /// </summary>
    /// <param name="slope">バイアスの傾き（シングルヘッド用）。デフォルトは1.0</param>
    /// <remarks>
    /// マルチヘッドアテンションの場合、各ヘッドで異なるslopeを使用しますが、
    /// このシングルヘッド実装では単一のslopeを使用します。
    /// 一般的なマルチヘッドでは slope = 2^(-8/n) * 2^(-head_index) のような値を使用します。
    /// </remarks>
    public ALiBiPositionalEncoding(float slope = 1.0f)
    {
        _slope = slope;
    }

    /// <inheritdoc/>
    public (Tensor query, Tensor key) ApplyToQueryKey(Tensor query, Tensor key, int positionOffset = 0)
    {
        throw new NotSupportedException("ALiBiはScoreBiasタイプのため、ApplyToQueryKeyはサポートされていません。");
    }

    /// <inheritdoc/>
    public Tensor GetScoreBias(int queryLength, int keyLength, Device device, ScalarType dtype)
    {
        if (_biasCache is not null
            && _cachedQueryLength == queryLength
            && _cachedKeyLength == keyLength
            && _biasCache.device == device
            && _biasCache.dtype == dtype)
        {
            return _biasCache;
        }

        _biasCache?.Dispose();
        _biasCache = BuildBias(queryLength, keyLength, device, dtype);
        _cachedQueryLength = queryLength;
        _cachedKeyLength = keyLength;

        return _biasCache;
    }

    /// <summary>
    /// ALiBiバイアステンソルを構築します。
    /// </summary>
    /// <param name="queryLength">Queryのシーケンス長</param>
    /// <param name="keyLength">Keyのシーケンス長</param>
    /// <param name="device">デバイス</param>
    /// <param name="dtype">データ型</param>
    /// <returns>バイアステンソル [queryLength, keyLength]</returns>
    private Tensor BuildBias(int queryLength, int keyLength, Device device, ScalarType dtype)
    {
        // ALiBiは相対位置に基づくバイアスを計算
        // bias[i, j] = -slope * |i - j| (因果的マスクの場合は i - j >= 0 の範囲のみ)
        // ここでは非因果的（双方向）のバイアスを計算

        using var queryPositions = torch.arange(queryLength, dtype: ScalarType.Float32, device: device).unsqueeze(1);
        using var keyPositions = torch.arange(keyLength, dtype: ScalarType.Float32, device: device).unsqueeze(0);

        // 相対距離を計算
        var relativePositions = queryPositions - keyPositions;

        // 負の傾きを適用（遠い位置ほどペナルティ）
        var bias = -_slope * torch.abs(relativePositions);

        return bias.to(dtype);
    }

    /// <summary>
    /// キャッシュを解放します。
    /// </summary>
    public void ClearCache()
    {
        _biasCache?.Dispose();
        _biasCache = null;
        _cachedQueryLength = 0;
        _cachedKeyLength = 0;
    }
}