// zlib License
// 
// Copyright (c) 2025 Sinoa
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

using MLModelUtility.Models;
using MLModelUtility.Models.Graph;

namespace MLModelUtility.IO;

/// <summary>
/// 計算グラフの書き込みインターフェース
/// </summary>
public interface IGraphWriter
{
    /// <summary>
    /// 計算グラフをストリームに書き込む
    /// </summary>
    /// <param name="graph">書き込む計算グラフ</param>
    /// <param name="stream">出力ストリーム</param>
    void WriteGraph(ComputeGraph graph, Stream stream);

    /// <summary>
    /// 計算グラフをストリームに非同期で書き込む
    /// </summary>
    /// <param name="graph">書き込む計算グラフ</param>
    /// <param name="stream">出力ストリーム</param>
    /// <param name="cancellationToken">キャンセルトークン</param>
    Task WriteGraphAsync(ComputeGraph graph, Stream stream, CancellationToken cancellationToken = default);

    /// <summary>
    /// 計算グラフをファイルに書き込む
    /// </summary>
    /// <param name="graph">書き込む計算グラフ</param>
    /// <param name="filePath">ファイルパス</param>
    void WriteGraphToFile(ComputeGraph graph, string filePath);

    /// <summary>
    /// 計算グラフをファイルに非同期で書き込む
    /// </summary>
    /// <param name="graph">書き込む計算グラフ</param>
    /// <param name="filePath">ファイルパス</param>
    /// <param name="cancellationToken">キャンセルトークン</param>
    Task WriteGraphToFileAsync(ComputeGraph graph, string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// 計算グラフとテンソルを同時にストリームに書き込む
    /// </summary>
    /// <param name="graph">書き込む計算グラフ</param>
    /// <param name="tensors">書き込むテンソルコレクション</param>
    /// <param name="stream">出力ストリーム</param>
    void WriteGraphWithTensors(ComputeGraph graph, TensorCollection tensors, Stream stream);

    /// <summary>
    /// 計算グラフとテンソルを同時にストリームに非同期で書き込む
    /// </summary>
    /// <param name="graph">書き込む計算グラフ</param>
    /// <param name="tensors">書き込むテンソルコレクション</param>
    /// <param name="stream">出力ストリーム</param>
    /// <param name="cancellationToken">キャンセルトークン</param>
    Task WriteGraphWithTensorsAsync(
        ComputeGraph graph, TensorCollection tensors, Stream stream, CancellationToken cancellationToken = default);
}