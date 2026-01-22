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

// ReSharper disable OptionalParameterHierarchyMismatch

using Foxtamp.MLCoreModule.PositionalEncodings;
using TorchSharp.Modules;
using TorchSharp.Utils;
using static TorchSharp.torch;
using static TorchSharp.torch.nn;

namespace Foxtamp.MLCoreModule.Modules;

/// <summary>
/// アテンションモジュールの基底クラス。
/// Query/Key/Value投影、位置エンコーディング、ScaledDotProductAttentionの共通処理を提供します。
/// </summary>
public abstract class AttentionBase : Module<Tensor, Tensor, Tensor, Tensor?, int, Tensor>
{
    /// <summary>
    /// Query投影用の線形層
    /// </summary>
    [ComponentName(Name = "query_projection")]
    private readonly Linear _queryProjection;

    /// <summary>
    /// Key投影用の線形層
    /// </summary>
    [ComponentName(Name = "key_projection")]
    private readonly Linear _keyProjection;

    /// <summary>
    /// Value投影用の線形層
    /// </summary>
    [ComponentName(Name = "value_projection")]
    private readonly Linear _valueProjection;

    /// <summary>
    /// 出力投影用の線形層
    /// </summary>
    [ComponentName(Name = "output_projection")]
    private readonly Linear _outputProjection;

    /// <summary>
    /// Scaled Dot-Product Attentionモジュール
    /// </summary>
    [ComponentName(Name = "scaled_dot_product_attention")]
    private readonly ScaledDotProductAttention _attention;

    /// <summary>
    /// 位置エンコーディング（オプション）
    /// </summary>
    [ComponentName(Name = "positional_encoding")]
    private readonly IPositionalEncoding? _positionalEncoding;

    /// <summary>
    /// AttentionBaseインスタンスを初期化します。
    /// </summary>
    /// <param name="name">モジュール名</param>
    /// <param name="embeddingDimension">入力/出力の埋め込み次元数</param>
    /// <param name="queryDimension">Query/Keyの内部次元数（nullの場合はembeddingDimensionを使用）</param>
    /// <param name="valueDimension">Valueの内部次元数（nullの場合はembeddingDimensionを使用）</param>
    /// <param name="dropoutProbability">アテンション重みのドロップアウト確率（0.0で無効化）</param>
    /// <param name="positionalEncoding">位置エンコーディング（nullで無効化）</param>
    /// <param name="useBias">線形層にバイアスを使用するかどうか</param>
    protected AttentionBase(
        string name,
        int embeddingDimension,
        int? queryDimension = null,
        int? valueDimension = null,
        float dropoutProbability = 0.0f,
        IPositionalEncoding? positionalEncoding = null,
        bool useBias = false) : base(name)
    {
        var qkDim = queryDimension ?? embeddingDimension;
        var vDim = valueDimension ?? embeddingDimension;

        // 線形投影層を作成
        _queryProjection = Linear(embeddingDimension, qkDim, hasBias: useBias);
        _keyProjection = Linear(embeddingDimension, qkDim, hasBias: useBias);
        _valueProjection = Linear(embeddingDimension, vDim, hasBias: useBias);
        _outputProjection = Linear(vDim, embeddingDimension, hasBias: useBias);

        // Scaled Dot-Product Attention
        _attention = new ScaledDotProductAttention(qkDim, dropoutProbability);

        // 位置エンコーディング
        _positionalEncoding = positionalEncoding;

        // ReSharper disable once VirtualMemberCallInConstructor
        RegisterComponents();
    }

    /// <summary>
    /// Query/Key/Valueテンソルを投影し、位置エンコーディングを適用してアテンション出力を計算します。
    /// </summary>
    /// <param name="queryInput">Query生成元の入力テンソル [batch, queryLen, embeddingDim]</param>
    /// <param name="keyInput">Key生成元の入力テンソル [batch, keyLen, embeddingDim]</param>
    /// <param name="valueInput">Value生成元の入力テンソル [batch, keyLen, embeddingDim]</param>
    /// <param name="mask">アテンションマスク（オプション）</param>
    /// <param name="positionOffset">位置オフセット（RoPE使用時のキャッシュ対応用）</param>
    /// <returns>アテンション出力 [batch, queryLen, embeddingDim]</returns>
    public override Tensor forward(Tensor queryInput, Tensor keyInput, Tensor valueInput, Tensor? mask = null, int positionOffset = 0)
    {
        // 線形投影
        var query = _queryProjection.forward(queryInput);
        var key = _keyProjection.forward(keyInput);
        var value = _valueProjection.forward(valueInput);

        // 位置エンコーディングの適用
        Tensor? scoreBias = null;
        if (_positionalEncoding is not null)
        {
            switch (_positionalEncoding.EncodingType)
            {
                case PositionalEncodingType.QueryKeyTransform:
                    // RoPE: Query/Keyに回転変換を適用
                    (query, key) = _positionalEncoding.ApplyToQueryKey(query, key, positionOffset);
                    break;

                case PositionalEncodingType.ScoreBias:
                    // ALiBi: スコアバイアスを取得
                    var queryLen = (int)query.shape[^2];
                    var keyLen = (int)key.shape[^2];
                    scoreBias = _positionalEncoding.GetScoreBias(queryLen, keyLen, query.device, query.dtype);
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        // Scaled Dot-Product Attention
        using var attentionOutput = _attention.forward(query, key, value, mask, scoreBias);

        // 出力投影
        return _outputProjection.forward(attentionOutput);
    }
}