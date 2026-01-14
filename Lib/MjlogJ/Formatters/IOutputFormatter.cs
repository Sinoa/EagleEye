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

using MjlogJ.Models;

namespace MjlogJ.Formatters;

/// <summary>
/// 出力フォーマッタのインターフェース
/// </summary>
public interface IOutputFormatter
{
    /// <summary>
    /// ファイル拡張子
    /// </summary>
    string FileExtension { get; }

    /// <summary>
    /// GameRecordをストリームに出力
    /// </summary>
    /// <param name="record">出力する試合記録</param>
    /// <param name="output">出力先ストリーム</param>
    void Format(GameRecord record, Stream output);

    /// <summary>
    /// GameRecordを文字列として出力
    /// </summary>
    /// <param name="record">出力する試合記録</param>
    /// <returns>フォーマットされた文字列</returns>
    string FormatToString(GameRecord record);

    /// <summary>
    /// 文字列からGameRecordを読み込み
    /// </summary>
    /// <param name="content">読み込む文字列</param>
    /// <returns>GameRecord</returns>
    GameRecord LoadFromString(string content);

    /// <summary>
    /// ストリームからGameRecordを読み込み
    /// </summary>
    /// <param name="input">入力ストリーム</param>
    /// <returns>GameRecord</returns>
    GameRecord LoadFromStream(Stream input);
}