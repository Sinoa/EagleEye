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
/// Attention with Linear Biases (ALiBi) の実装。
/// アテンションスコアに線形バイアスを加算して位置情報をエンコードします。
/// </summary>
/// <remarks>
/// 参考論文: "Train Short, Test Long: Attention with Linear Biases Enables Input Length Extrapolation"
/// https://arxiv.org/abs/2108.12409
/// </remarks>
public sealed class ALiBiPositionalEncoding : Module, IPositionalEncoding
{
    private readonly int _maxLength;
    private readonly Tensor _biasTable;

    /// <inheritdoc/>
    public PositionalEncodingType EncodingType => PositionalEncodingType.ScoreBias;

    /// <summary>
    /// ALiBiインスタンスを初期化します。
    /// </summary>
    /// <param name="maxLength">サポートする最大シーケンス長</param>
    /// <param name="slope">バイアスの傾き（シングルヘッド用）。デフォルトは1.0</param>
    /// <param name="device">テンソルを配置するデバイス（デフォルト: CPU）</param>
    /// <param name="dtype">テンソルのデータ型（デフォルト: float32）</param>
    /// <remarks>
    /// マルチヘッドアテンションの場合、各ヘッドで異なるslopeを使用しますが、
    /// このシングルヘッド実装では単一のslopeを使用します。
    /// 一般的なマルチヘッドでは slope = 2^(-8/n) * 2^(-head_index) のような値を使用します。
    /// </remarks>
    public ALiBiPositionalEncoding(int maxLength = 2048, float slope = 1.0f, Device? device = null, ScalarType dtype = ScalarType.Float32)
        : base(nameof(ALiBiPositionalEncoding))
    {
        _maxLength = maxLength;
        var targetDevice = device ?? CPU;

        // 事前にバイアステーブルを生成 [maxLength, maxLength]
        _biasTable = BuildBias(maxLength, maxLength, targetDevice, dtype, slope);

        // バッファとして登録（persistent=trueで永続化）
        register_buffer("alibi_bias", _biasTable, persistent: true);
    }

    /// <inheritdoc/>
    /// <exception cref="NotSupportedException">ALiBiはScoreBiasタイプのため、ApplyToQueryKeyはサポートされていません。</exception>
    public (Tensor query, Tensor key) ApplyToQueryKey(Tensor query, Tensor key, int positionOffset = 0)
    {
        throw new NotSupportedException("ALiBiはScoreBiasタイプのため、ApplyToQueryKeyはサポートされていません。");
    }

    /// <inheritdoc/>
    /// <exception cref="ArgumentOutOfRangeException">queryLength または keyLength が最大長を超えました。</exception>
    public Tensor GetScoreBias(int queryLength, int keyLength, Device device, ScalarType dtype)
    {
        if (queryLength > _maxLength || keyLength > _maxLength)
        {
            throw new ArgumentOutOfRangeException($"要求されたシーケンス長（query: {queryLength}, key: {keyLength}）が最大長（{_maxLength}）を超えています。");
        }

        // 事前生成したテーブルから必要な範囲をスライス
        var bias = _biasTable[TensorIndex.Slice(0, queryLength), TensorIndex.Slice(0, keyLength)];

        // デバイスまたはデータ型が異なる場合は変換
        if (bias.device != device || bias.dtype != dtype)
        {
            bias = bias.to(dtype, device);
        }

        return bias;
    }

    /// <summary>
    /// ALiBiバイアステンソルを構築します。
    /// </summary>
    /// <param name="queryLength">Queryのシーケンス長</param>
    /// <param name="keyLength">Keyのシーケンス長</param>
    /// <param name="device">デバイス</param>
    /// <param name="dtype">データ型</param>
    /// <param name="slope">バイアスの傾き</param>
    /// <returns>バイアステンソル [queryLength, keyLength]</returns>
    private static Tensor BuildBias(int queryLength, int keyLength, Device device, ScalarType dtype, float slope = 1.0f)
    {
        // ALiBiは相対位置に基づくバイアスを計算
        // bias[i, j] = -slope * |i - j| (因果的マスクの場合は i - j >= 0 の範囲のみ)
        // ここでは非因果的（双方向）のバイアスを計算
        using var queryPositions = arange(queryLength, dtype: ScalarType.Float32, device: device).unsqueeze(1);
        using var keyPositions = arange(keyLength, dtype: ScalarType.Float32, device: device).unsqueeze(0);

        // 相対距離を計算
        var relativePositions = queryPositions - keyPositions;

        // 負の傾きを適用（遠い位置ほどペナルティ）
        var bias = -slope * abs(relativePositions);
        return bias.to(dtype);
    }
}