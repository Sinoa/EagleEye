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

// ReSharper disable InconsistentNaming

using Foxtamp.MLCoreModule.Modules;
using Foxtamp.MLCoreModule.PositionalEncodings;
using static TorchSharp.torch;

namespace Foxtamp.MLCoreModule.Utilities;

/// <summary>
/// MLCoreモジュールで使用される各種モジュールを生成するためのユーティリティクラスを提供します。
/// </summary>
public static class MLCoreModuleUtility
{
    /// <summary>
    /// 標準的なセルフアテンション機構を生成します。
    /// </summary>
    /// <param name="embeddingDimension">埋め込みベクトルの次元数。</param>
    /// <param name="queryDimension">クエリの次元数。nullの場合は<paramref name="embeddingDimension"/>が使用されます。</param>
    /// <param name="valueDimension">値の次元数。nullの場合は<paramref name="embeddingDimension"/>が使用されます。</param>
    /// <param name="dropoutProbability">ドロップアウトの確率。デフォルトは0.0（ドロップアウトなし）。</param>
    /// <param name="useBias">バイアス項を使用するかどうか。デフォルトはfalse。</param>
    /// <returns>生成されたセルフアテンション機構のインスタンス。</returns>
    public static SelfAttention CreateSelfAttention(
        int embeddingDimension,
        int? queryDimension = null,
        int? valueDimension = null,
        float dropoutProbability = 0.0f,
        bool useBias = false)
    {
        return new SelfAttention(
            embeddingDimension: embeddingDimension,
            queryDimension: queryDimension,
            valueDimension: valueDimension,
            dropoutProbability: dropoutProbability,
            useBias: useBias);
    }

    /// <summary>
    /// Rotary Positional Encoding (RoPE) を使用したセルフアテンション機構を生成します。
    /// </summary>
    /// <param name="embeddingDimension">埋め込みベクトルの次元数。</param>
    /// <param name="maxSequenceLength">処理可能な最大シーケンス長。</param>
    /// <param name="queryDimension">クエリの次元数。nullの場合は<paramref name="embeddingDimension"/>が使用されます。</param>
    /// <param name="valueDimension">値の次元数。nullの場合は<paramref name="embeddingDimension"/>が使用されます。</param>
    /// <param name="dropoutProbability">ドロップアウトの確率。デフォルトは0.0（ドロップアウトなし）。</param>
    /// <param name="useBias">バイアス項を使用するかどうか。デフォルトはfalse。</param>
    /// <param name="baseFrequency">RoPEの基底周波数。デフォルトは10000.0。</param>
    /// <param name="device">計算を実行するデバイス。nullの場合はデフォルトデバイスが使用されます。</param>
    /// <param name="dtype">テンソルのデータ型。デフォルトはFloat32。</param>
    /// <returns>RoPEを使用したセルフアテンション機構のインスタンス。</returns>
    public static SelfAttention CreateSelfAttentionWithRope(
        int embeddingDimension,
        int maxSequenceLength,
        int? queryDimension = null,
        int? valueDimension = null,
        float dropoutProbability = 0.0f,
        bool useBias = false,
        float baseFrequency = 10000.0f,
        Device? device = null,
        ScalarType dtype = ScalarType.Float32)
    {
        var rope = new RotaryPositionalEncoding(
            dimension: embeddingDimension,
            maxSequenceLength: maxSequenceLength,
            baseFrequency: baseFrequency,
            device: device,
            dtype: dtype);

        return new SelfAttention(
            embeddingDimension: embeddingDimension,
            queryDimension: queryDimension,
            valueDimension: valueDimension,
            dropoutProbability: dropoutProbability,
            positionalEncoding: rope,
            useBias: useBias);
    }

    /// <summary>
    /// Attention with Linear Biases (ALiBi) を使用したセルフアテンション機構を生成します。
    /// </summary>
    /// <param name="embeddingDimension">埋め込みベクトルの次元数。</param>
    /// <param name="maxLength">処理可能な最大長。</param>
    /// <param name="queryDimension">クエリの次元数。nullの場合は<paramref name="embeddingDimension"/>が使用されます。</param>
    /// <param name="valueDimension">値の次元数。nullの場合は<paramref name="embeddingDimension"/>が使用されます。</param>
    /// <param name="dropoutProbability">ドロップアウトの確率。デフォルトは0.0（ドロップアウトなし）。</param>
    /// <param name="useBias">バイアス項を使用するかどうか。デフォルトはfalse。</param>
    /// <param name="slope">ALiBiの傾き。デフォルトは1.0。</param>
    /// <param name="device">計算を実行するデバイス。nullの場合はデフォルトデバイスが使用されます。</param>
    /// <param name="dtype">テンソルのデータ型。デフォルトはFloat32。</param>
    /// <returns>ALiBiを使用したセルフアテンション機構のインスタンス。</returns>
    public static SelfAttention CreateSelfAttentionWithAlibi(
        int embeddingDimension,
        int maxLength,
        int? queryDimension = null,
        int? valueDimension = null,
        float dropoutProbability = 0.0f,
        bool useBias = false,
        float slope = 1.0f,
        Device? device = null,
        ScalarType dtype = ScalarType.Float32)
    {
        var alibi = new ALiBiPositionalEncoding(
            maxLength: maxLength,
            slope: slope,
            device: device,
            dtype: dtype);

        return new SelfAttention(
            embeddingDimension: embeddingDimension,
            queryDimension: queryDimension,
            valueDimension: valueDimension,
            dropoutProbability: dropoutProbability,
            positionalEncoding: alibi,
            useBias: useBias);
    }

    /// <summary>
    /// 標準的なクロスアテンション機構を生成します。
    /// </summary>
    /// <param name="embeddingDimension">埋め込みベクトルの次元数。</param>
    /// <param name="queryDimension">クエリの次元数。nullの場合は<paramref name="embeddingDimension"/>が使用されます。</param>
    /// <param name="valueDimension">値の次元数。nullの場合は<paramref name="embeddingDimension"/>が使用されます。</param>
    /// <param name="dropoutProbability">ドロップアウトの確率。デフォルトは0.0（ドロップアウトなし）。</param>
    /// <param name="useBias">バイアス項を使用するかどうか。デフォルトはfalse。</param>
    /// <returns>生成されたクロスアテンション機構のインスタンス。</returns>
    public static CrossAttention CreateCrossAttention(
        int embeddingDimension,
        int? queryDimension = null,
        int? valueDimension = null,
        float dropoutProbability = 0.0f,
        bool useBias = false)
    {
        return new CrossAttention(
            embeddingDimension: embeddingDimension,
            queryDimension: queryDimension,
            valueDimension: valueDimension,
            dropoutProbability: dropoutProbability,
            useBias: useBias);
    }

    /// <summary>
    /// Rotary Positional Encoding (RoPE) を使用したクロスアテンション機構を生成します。
    /// </summary>
    /// <param name="embeddingDimension">埋め込みベクトルの次元数。</param>
    /// <param name="maxSequenceLength">処理可能な最大シーケンス長。</param>
    /// <param name="queryDimension">クエリの次元数。nullの場合は<paramref name="embeddingDimension"/>が使用されます。</param>
    /// <param name="valueDimension">値の次元数。nullの場合は<paramref name="embeddingDimension"/>が使用されます。</param>
    /// <param name="dropoutProbability">ドロップアウトの確率。デフォルトは0.0（ドロップアウトなし）。</param>
    /// <param name="useBias">バイアス項を使用するかどうか。デフォルトはfalse。</param>
    /// <param name="baseFrequency">RoPEの基底周波数。デフォルトは10000.0。</param>
    /// <param name="device">計算を実行するデバイス。nullの場合はデフォルトデバイスが使用されます。</param>
    /// <param name="dtype">テンソルのデータ型。デフォルトはFloat32。</param>
    /// <returns>RoPEを使用したクロスアテンション機構のインスタンス。</returns>
    public static CrossAttention CreateCrossAttentionWithRope(
        int embeddingDimension,
        int maxSequenceLength,
        int? queryDimension = null,
        int? valueDimension = null,
        float dropoutProbability = 0.0f,
        bool useBias = false,
        float baseFrequency = 10000.0f,
        Device? device = null,
        ScalarType dtype = ScalarType.Float32)
    {
        var rope = new RotaryPositionalEncoding(
            dimension: embeddingDimension,
            maxSequenceLength: maxSequenceLength,
            baseFrequency: baseFrequency,
            device: device,
            dtype: dtype);

        return new CrossAttention(
            embeddingDimension: embeddingDimension,
            queryDimension: queryDimension,
            valueDimension: valueDimension,
            dropoutProbability: dropoutProbability,
            positionalEncoding: rope,
            useBias: useBias);
    }

    /// <summary>
    /// Attention with Linear Biases (ALiBi) を使用したクロスアテンション機構を生成します。
    /// </summary>
    /// <param name="embeddingDimension">埋め込みベクトルの次元数。</param>
    /// <param name="maxLength">処理可能な最大長。</param>
    /// <param name="queryDimension">クエリの次元数。nullの場合は<paramref name="embeddingDimension"/>が使用されます。</param>
    /// <param name="valueDimension">値の次元数。nullの場合は<paramref name="embeddingDimension"/>が使用されます。</param>
    /// <param name="dropoutProbability">ドロップアウトの確率。デフォルトは0.0（ドロップアウトなし）。</param>
    /// <param name="useBias">バイアス項を使用するかどうか。デフォルトはfalse。</param>
    /// <param name="slope">ALiBiの傾き。デフォルトは1.0。</param>
    /// <param name="device">計算を実行するデバイス。nullの場合はデフォルトデバイスが使用されます。</param>
    /// <param name="dtype">テンソルのデータ型。デフォルトはFloat32。</param>
    /// <returns>ALiBiを使用したクロスアテンション機構のインスタンス。</returns>
    public static CrossAttention CreateCrossAttentionWithAlibi(
        int embeddingDimension,
        int maxLength,
        int? queryDimension = null,
        int? valueDimension = null,
        float dropoutProbability = 0.0f,
        bool useBias = false,
        float slope = 1.0f,
        Device? device = null,
        ScalarType dtype = ScalarType.Float32)
    {
        var alibi = new ALiBiPositionalEncoding(
            maxLength: maxLength,
            slope: slope,
            device: device,
            dtype: dtype);

        return new CrossAttention(
            embeddingDimension: embeddingDimension,
            queryDimension: queryDimension,
            valueDimension: valueDimension,
            dropoutProbability: dropoutProbability,
            positionalEncoding: alibi,
            useBias: useBias);
    }
}