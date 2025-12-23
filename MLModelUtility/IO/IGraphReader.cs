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
/// 計算グラフの読み込みインターフェース
/// </summary>
public interface IGraphReader
{
    /// <summary>
    /// ストリームから計算グラフを読み込む
    /// </summary>
    /// <param name="stream">入力ストリーム</param>
    /// <returns>読み込まれた計算グラフ</returns>
    ComputeGraph ReadGraph(Stream stream);

    /// <summary>
    /// ストリームから計算グラフを非同期で読み込む
    /// </summary>
    /// <param name="stream">入力ストリーム</param>
    /// <param name="cancellationToken">キャンセルトークン</param>
    /// <returns>読み込まれた計算グラフ</returns>
    Task<ComputeGraph> ReadGraphAsync(Stream stream, CancellationToken cancellationToken = default);

    /// <summary>
    /// ファイルから計算グラフを読み込む
    /// </summary>
    /// <param name="filePath">ファイルパス</param>
    /// <returns>読み込まれた計算グラフ</returns>
    ComputeGraph ReadGraphFromFile(string filePath);

    /// <summary>
    /// ファイルから計算グラフを非同期で読み込む
    /// </summary>
    /// <param name="filePath">ファイルパス</param>
    /// <param name="cancellationToken">キャンセルトークン</param>
    /// <returns>読み込まれた計算グラフ</returns>
    Task<ComputeGraph> ReadGraphFromFileAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// ストリームから計算グラフとテンソルを同時に読み込む
    /// </summary>
    /// <param name="stream">入力ストリーム</param>
    /// <returns>計算グラフとテンソルコレクションのタプル</returns>
    (ComputeGraph Graph, TensorCollection Tensors) ReadGraphWithTensors(Stream stream);

    /// <summary>
    /// ストリームから計算グラフとテンソルを同時に非同期で読み込む
    /// </summary>
    /// <param name="stream">入力ストリーム</param>
    /// <param name="cancellationToken">キャンセルトークン</param>
    /// <returns>計算グラフとテンソルコレクションのタプル</returns>
    Task<(ComputeGraph Graph, TensorCollection Tensors)> ReadGraphWithTensorsAsync(
        Stream stream, CancellationToken cancellationToken = default);
}