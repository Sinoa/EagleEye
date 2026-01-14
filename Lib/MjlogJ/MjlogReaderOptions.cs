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
using MjlogJ.Validation;

namespace MjlogJ;

/// <summary>
/// MjlogReader の読み込みオプション
/// </summary>
public class MjlogReaderOptions
{
    /// <summary>
    /// 並列処理の最大並列度（デフォルト: プロセッサ数）
    /// </summary>
    public int MaxDegreeOfParallelism { get; set; } = Environment.ProcessorCount;

    /// <summary>
    /// エラー発生時に処理を続行するか（デフォルト: true）
    /// </summary>
    public bool ContinueOnError { get; set; } = true;

    /// <summary>
    /// バリデーションを実行するか（デフォルト: false）
    /// </summary>
    public bool EnableValidation { get; set; } = false;

    /// <summary>
    /// GZip圧縮を自動検出するか（デフォルト: true）
    /// .mjlog拡張子のファイルはGZip圧縮として扱う
    /// </summary>
    public bool AutoDetectGzip { get; set; } = true;

    /// <summary>
    /// キャンセルトークン
    /// </summary>
    public CancellationToken CancellationToken { get; set; } = default;

    /// <summary>
    /// ファイル検索パターン（LoadManyAsync使用時）
    /// </summary>
    public string SearchPattern { get; set; } = "*.xml";

    /// <summary>
    /// サブディレクトリも検索するか（デフォルト: true）
    /// </summary>
    public bool IncludeSubdirectories { get; set; } = true;

    /// <summary>
    /// デフォルトオプションを取得
    /// </summary>
    public static MjlogReaderOptions Default => new();
}

/// <summary>
/// 複数ファイル読み込み時の結果
/// </summary>
public class LoadResult
{
    /// <summary>ファイルパス</summary>
    public required string FilePath { get; init; }

    /// <summary>読み込んだGameRecord（成功時）</summary>
    public GameRecord? Record { get; init; }

    /// <summary>読み込みが成功したか</summary>
    public bool IsSuccess => Record != null && Error == null;

    /// <summary>エラー情報（失敗時）</summary>
    public Exception? Error { get; init; }

    /// <summary>バリデーション結果（オプション有効時）</summary>
    public ValidationResult? Validation { get; init; }
}