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

namespace Foxtamp.MLCoreModule.Modules;

/// <summary>
/// シングルヘッド・クロスアテンションモジュール。
/// Queryは一方の入力から、Key/Valueは別のコンテキストから生成してアテンションを計算します。
/// </summary>
/// <remarks>
/// クロスアテンションは、Encoder-Decoderアーキテクチャなどで使用されます。
/// 例: DecoderのQueryがEncoderの出力（コンテキスト）に対してアテンションを行う。
/// </remarks>
public sealed class CrossAttention : AttentionBase
{
    /// <summary>
    /// CrossAttentionインスタンスを初期化します。
    /// </summary>
    /// <param name="embeddingDimension">入力/出力の埋め込み次元数</param>
    /// <param name="queryDimension">Query/Keyの内部次元数（nullの場合はembeddingDimensionを使用）</param>
    /// <param name="valueDimension">Valueの内部次元数（nullの場合はembeddingDimensionを使用）</param>
    /// <param name="dropoutProbability">アテンション重みのドロップアウト確率（0.0で無効化）</param>
    /// <param name="positionalEncoding">位置エンコーディング（nullで無効化）</param>
    /// <param name="useBias">線形変換にバイアスを使用するかどうか</param>
    public CrossAttention(
        int embeddingDimension,
        int? queryDimension = null,
        int? valueDimension = null,
        float dropoutProbability = 0.0f,
        IPositionalEncoding? positionalEncoding = null,
        bool useBias = false)
        : base(
            nameof(CrossAttention),
            embeddingDimension,
            queryDimension,
            valueDimension,
            dropoutProbability,
            positionalEncoding,
            useBias)
    {
    }
}