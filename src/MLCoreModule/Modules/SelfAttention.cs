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

using Foxtamp.MLCoreModule.PositionalEncodings;
using static TorchSharp.torch;

namespace Foxtamp.MLCoreModule.Modules;

/// <summary>
/// シングルヘッド・セルフアテンションモジュール。
/// 入力シーケンス自身に対してアテンションを計算します。
/// </summary>
/// <remarks>
/// セルフアテンションでは、Query/Key/Valueすべてが同一の入力から生成されます。
/// </remarks>
public sealed class SelfAttention : AttentionBase
{
    /// <summary>
    /// SelfAttentionインスタンスを初期化します。
    /// </summary>
    /// <param name="embeddingDimension">入力/出力の埋め込み次元数</param>
    /// <param name="queryDimension">Query/Keyの内部次元数（nullの場合はembeddingDimensionを使用）</param>
    /// <param name="valueDimension">Valueの内部次元数（nullの場合はembeddingDimensionを使用）</param>
    /// <param name="dropoutProbability">アテンション重みのドロップアウト確率（0.0で無効化）</param>
    /// <param name="positionalEncoding">位置エンコーディング（nullで無効化）</param>
    public SelfAttention(
        int embeddingDimension,
        int? queryDimension = null,
        int? valueDimension = null,
        float dropoutProbability = 0.0f,
        IPositionalEncoding? positionalEncoding = null)
        : base(
            nameof(SelfAttention),
            embeddingDimension,
            queryDimension,
            valueDimension,
            dropoutProbability,
            positionalEncoding)
    {
    }

    /// <summary>
    /// セルフアテンションを計算します。
    /// </summary>
    /// <param name="input">入力テンソル [batch, seqLen, embeddingDim]</param>
    /// <param name="mask">アテンションマスク [seqLen, seqLen] または [batch, seqLen, seqLen]（オプション）。
    /// Trueの位置がマスクされます。</param>
    /// <param name="positionOffset">位置オフセット（RoPE使用時のキャッシュ対応用）</param>
    /// <returns>アテンション出力 [batch, seqLen, embeddingDim]</returns>
    // ReSharper disable once InconsistentNaming
    public Tensor forward(Tensor input, Tensor? mask = null, int positionOffset = 0)
    {
        // セルフアテンション: Query/Key/Valueすべて同一入力から生成
        return base.forward(input, input, input, mask, positionOffset);
    }
}