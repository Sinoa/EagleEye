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

namespace Foxtamp.MLCoreModule.PositionalEncodings;

/// <summary>
/// 位置エンコーディングの適用タイプ
/// </summary>
public enum PositionalEncodingType
{
    /// <summary>
    /// Query/Keyテンソルに変換を適用するタイプ（RoPEなど）
    /// </summary>
    QueryKeyTransform,

    /// <summary>
    /// アテンションスコアにバイアスを加算するタイプ（ALiBiなど）
    /// </summary>
    ScoreBias
}

/// <summary>
/// 位置エンコーディングのインターフェース
/// </summary>
public interface IPositionalEncoding
{
    /// <summary>
    /// 位置エンコーディングの適用タイプ
    /// </summary>
    PositionalEncodingType EncodingType { get; }

    /// <summary>
    /// Query/Keyテンソルに位置エンコーディングを適用します。
    /// <see cref="EncodingType"/>が<see cref="PositionalEncodingType.QueryKeyTransform"/>の場合に使用します。
    /// </summary>
    /// <param name="query">Queryテンソル [batch, seqLen, dim]</param>
    /// <param name="key">Keyテンソル [batch, seqLen, dim]</param>
    /// <param name="positionOffset">位置オフセット（キャッシュ使用時など）</param>
    /// <returns>位置エンコーディング適用後の(Query, Key)タプル</returns>
    (Tensor query, Tensor key) ApplyToQueryKey(Tensor query, Tensor key, int positionOffset = 0);

    /// <summary>
    /// アテンションスコアに加算するバイアステンソルを取得します。
    /// <see cref="EncodingType"/>が<see cref="PositionalEncodingType.ScoreBias"/>の場合に使用します。
    /// </summary>
    /// <param name="queryLength">Queryのシーケンス長</param>
    /// <param name="keyLength">Keyのシーケンス長</param>
    /// <param name="device">デバイス</param>
    /// <param name="dtype">データ型</param>
    /// <returns>スコアバイアステンソル [queryLength, keyLength]</returns>
    Tensor GetScoreBias(int queryLength, int keyLength, Device device, ScalarType dtype);
}