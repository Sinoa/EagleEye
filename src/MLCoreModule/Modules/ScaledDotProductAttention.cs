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

using TorchSharp.Modules;
using static TorchSharp.torch;
using static TorchSharp.torch.nn;

namespace Foxtamp.MLCoreModule.Modules;

/// <summary>
/// Scaled Dot-Product Attention の実装。
/// </summary>
/// <remarks>
/// アテンション計算: Attention(Q, K, V) = softmax(QK^T / √d_k + mask + bias) V
/// </remarks>
public sealed class ScaledDotProductAttention : Module
{
    /// <summary>
    /// マスク位置に適用する負の大きな値。
    /// 数値安定性のため、NegativeInfinityではなく-1e9を使用。
    /// </summary>
    private const float MaskValue = -1e9f;

    private readonly Dropout? _dropout;
    private readonly float _scale;

    /// <summary>
    /// ScaledDotProductAttentionインスタンスを初期化します。
    /// </summary>
    /// <param name="dimension">Key/Queryの次元数（スケーリング係数の計算に使用）</param>
    /// <param name="dropoutProbability">ドロップアウト確率（0.0で無効化）</param>
    public ScaledDotProductAttention(int dimension, float dropoutProbability = 0.0f) : base(nameof(ScaledDotProductAttention))
    {
        _scale = 1.0f / MathF.Sqrt(dimension);

        if (dropoutProbability > 0.0f)
        {
            _dropout = Dropout(dropoutProbability);
            RegisterComponents();
        }
    }

    /// <summary>
    /// Scaled Dot-Product Attentionを計算します。
    /// </summary>
    /// <param name="query">Queryテンソル [batch, queryLen, dim]</param>
    /// <param name="key">Keyテンソル [batch, keyLen, dim]</param>
    /// <param name="value">Valueテンソル [batch, keyLen, valueDim]</param>
    /// <param name="mask">アテンションマスク [queryLen, keyLen] または [batch, queryLen, keyLen]（オプション）。
    /// Trueの位置がマスクされます（-infが加算されます）。</param>
    /// <param name="scoreBias">スコアバイアス [queryLen, keyLen]（オプション）。ALiBi等で使用。</param>
    /// <returns>アテンション出力 [batch, queryLen, valueDim]</returns>
    // ReSharper disable once InconsistentNaming
    public Tensor forward(Tensor query, Tensor key, Tensor value, Tensor? mask = null, Tensor? scoreBias = null)
    {
        // スコア計算: QK^T / √d_k
        // query: [batch, queryLen, dim]
        // key: [batch, keyLen, dim]
        // scores: [batch, queryLen, keyLen]
        var scores = matmul(query, key.transpose(-2, -1)) * _scale;

        // スコアバイアスを加算（ALiBi等）
        if (scoreBias is not null)
        {
            scores += scoreBias;
        }

        // マスクを適用
        if (mask is not null)
        {
            // boolマスクの場合、Trueの位置に大きな負の値を設定
            if (mask.dtype == ScalarType.Bool)
            {
                scores = scores.masked_fill(mask, MaskValue);
            }
            else
            {
                // floatマスクの場合は直接加算（大きな負の値が含まれている想定）
                scores += mask;
            }
        }

        // Softmax
        var attentionWeights = functional.softmax(scores, dim: -1);

        // Dropout（有効な場合のみ）
        if (_dropout is not null)
        {
            attentionWeights = _dropout.forward(attentionWeights);
        }

        // 出力計算: weights * V
        // attentionWeights: [batch, queryLen, keyLen]
        // value: [batch, keyLen, valueDim]
        // output: [batch, queryLen, valueDim]
        return matmul(attentionWeights, value);
    }
}