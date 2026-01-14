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

namespace MLModelUtility.IO;

/// <summary>
/// テンソルデータの読み込みインターフェース
/// </summary>
public interface ITensorReader
{
    /// <summary>
    /// ストリームからテンソルコレクションを読み込む
    /// </summary>
    /// <param name="stream">入力ストリーム</param>
    /// <returns>読み込まれたテンソルコレクション</returns>
    TensorCollection ReadTensors(Stream stream);

    /// <summary>
    /// ストリームからテンソルコレクションを非同期で読み込む
    /// </summary>
    /// <param name="stream">入力ストリーム</param>
    /// <param name="cancellationToken">キャンセルトークン</param>
    /// <returns>読み込まれたテンソルコレクション</returns>
    Task<TensorCollection> ReadTensorsAsync(Stream stream, CancellationToken cancellationToken = default);

    /// <summary>
    /// ファイルからテンソルコレクションを読み込む
    /// </summary>
    /// <param name="filePath">ファイルパス</param>
    /// <returns>読み込まれたテンソルコレクション</returns>
    TensorCollection ReadTensorsFromFile(string filePath);

    /// <summary>
    /// ファイルからテンソルコレクションを非同期で読み込む
    /// </summary>
    /// <param name="filePath">ファイルパス</param>
    /// <param name="cancellationToken">キャンセルトークン</param>
    /// <returns>読み込まれたテンソルコレクション</returns>
    Task<TensorCollection> ReadTensorsFromFileAsync(string filePath, CancellationToken cancellationToken = default);
}