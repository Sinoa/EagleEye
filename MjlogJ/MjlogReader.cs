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

using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using MjlogJ.Models;
using MjlogJ.Parsers;
using MjlogJ.Validation;

namespace MjlogJ;

/// <summary>
/// 天鳳牌譜（mjlog）を読み込むためのファサードAPI
/// </summary>
public static class MjlogReader
{
    private static readonly MjlogXmlParser DefaultParser = new();
    private static readonly GameRecordValidator DefaultValidator = new();

    #region 同期API - 単一ファイル

    /// <summary>
    /// ファイルから牌譜を読み込み（GZip自動判定）
    /// </summary>
    /// <param name="path">ファイルパス</param>
    /// <returns>GameRecord</returns>
    public static GameRecord Load(string path)
    {
        if (IsGzipFile(path))
        {
            return LoadFromGzip(path);
        }

        var content = File.ReadAllText(path);
        return Parse(content);
    }

    /// <summary>
    /// GZip圧縮ファイルから牌譜を読み込み
    /// </summary>
    /// <param name="path">ファイルパス（.mjlogなど）</param>
    /// <returns>GameRecord</returns>
    public static GameRecord LoadFromGzip(string path)
    {
        using var fileStream = File.OpenRead(path);
        using var gzipStream = new GZipStream(fileStream, CompressionMode.Decompress);
        using var reader = new StreamReader(gzipStream);
        var content = reader.ReadToEnd();
        return Parse(content);
    }

    /// <summary>
    /// XML文字列から牌譜をパース
    /// </summary>
    /// <param name="xmlContent">XML文字列</param>
    /// <returns>GameRecord</returns>
    public static GameRecord Parse(string xmlContent)
    {
        return DefaultParser.Parse(xmlContent);
    }

    /// <summary>
    /// ストリームから牌譜を読み込み
    /// </summary>
    /// <param name="stream">入力ストリーム</param>
    /// <returns>GameRecord</returns>
    public static GameRecord Parse(Stream stream)
    {
        using var reader = new StreamReader(stream);
        var content = reader.ReadToEnd();
        return Parse(content);
    }

    /// <summary>
    /// バリデーション付きで読み込み
    /// </summary>
    /// <param name="path">ファイルパス</param>
    /// <returns>GameRecordとバリデーション結果のタプル</returns>
    public static (GameRecord Record, ValidationResult Validation) LoadWithValidation(string path)
    {
        var record = Load(path);
        var validation = DefaultValidator.Validate(record);
        return (record, validation);
    }

    #endregion

    #region 非同期API - 単一ファイル

    /// <summary>
    /// 非同期でファイルから牌譜を読み込み（GZip自動判定）
    /// </summary>
    /// <param name="path">ファイルパス</param>
    /// <param name="cancellationToken">キャンセルトークン</param>
    /// <returns>GameRecord</returns>
    public static async Task<GameRecord> LoadAsync(string path, CancellationToken cancellationToken = default)
    {
        if (IsGzipFile(path))
        {
            return await LoadFromGzipAsync(path, cancellationToken);
        }

        var content = await File.ReadAllTextAsync(path, cancellationToken);
        return Parse(content);
    }

    /// <summary>
    /// 非同期でGZip圧縮ファイルから牌譜を読み込み
    /// </summary>
    /// <param name="path">ファイルパス</param>
    /// <param name="cancellationToken">キャンセルトークン</param>
    /// <returns>GameRecord</returns>
    public static async Task<GameRecord> LoadFromGzipAsync(string path, CancellationToken cancellationToken = default)
    {
        await using var fileStream = File.OpenRead(path);
        await using var gzipStream = new GZipStream(fileStream, CompressionMode.Decompress);
        using var reader = new StreamReader(gzipStream);
        var content = await reader.ReadToEndAsync(cancellationToken);
        return Parse(content);
    }

    /// <summary>
    /// 非同期でストリームから牌譜を読み込み
    /// </summary>
    /// <param name="stream">入力ストリーム</param>
    /// <param name="cancellationToken">キャンセルトークン</param>
    /// <returns>GameRecord</returns>
    public static async Task<GameRecord> ParseAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        using var reader = new StreamReader(stream);
        var content = await reader.ReadToEndAsync(cancellationToken);
        return Parse(content);
    }

    /// <summary>
    /// 非同期でバリデーション付き読み込み
    /// </summary>
    /// <param name="path">ファイルパス</param>
    /// <param name="cancellationToken">キャンセルトークン</param>
    /// <returns>GameRecordとバリデーション結果のタプル</returns>
    public static async Task<(GameRecord Record, ValidationResult Validation)> LoadWithValidationAsync(
        string path, CancellationToken cancellationToken = default)
    {
        var record = await LoadAsync(path, cancellationToken);
        var validation = DefaultValidator.Validate(record);
        return (record, validation);
    }

    #endregion

    #region ストリーミングAPI - 複数ファイル（順序保証）

    /// <summary>
    /// ディレクトリから複数ファイルを逐次読み込み（順序保証）
    /// </summary>
    /// <param name="directoryPath">ディレクトリパス</param>
    /// <param name="options">オプション</param>
    /// <returns>GameRecordの非同期列挙</returns>
    public static async IAsyncEnumerable<GameRecord> LoadManyAsync(
        string directoryPath,
        MjlogReaderOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        options ??= MjlogReaderOptions.Default;
        var searchOption = options.IncludeSubdirectories
            ? SearchOption.AllDirectories
            : SearchOption.TopDirectoryOnly;

        var files = Directory.GetFiles(directoryPath, options.SearchPattern, searchOption)
            .OrderBy(f => f);

        await foreach (var record in LoadManyAsync(files, options, cancellationToken))
        {
            yield return record;
        }
    }

    /// <summary>
    /// パスリストから複数ファイルを逐次読み込み（順序保証）
    /// </summary>
    /// <param name="paths">ファイルパスのリスト</param>
    /// <param name="options">オプション</param>
    /// <returns>GameRecordの非同期列挙</returns>
    public static async IAsyncEnumerable<GameRecord> LoadManyAsync(
        IEnumerable<string> paths,
        MjlogReaderOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        options ??= MjlogReaderOptions.Default;
        var token = options.CancellationToken != default ? options.CancellationToken : cancellationToken;

        foreach (var path in paths)
        {
            token.ThrowIfCancellationRequested();

            GameRecord? record = null;
            try
            {
                record = await LoadAsync(path, token);
            }
            catch (Exception) when (options.ContinueOnError)
            {
                // エラーを無視して続行
                continue;
            }

            if (record != null)
            {
                yield return record;
            }
        }
    }

    /// <summary>
    /// 複数ファイルをエラー情報付きで逐次読み込み（順序保証）
    /// </summary>
    /// <param name="paths">ファイルパスのリスト</param>
    /// <param name="options">オプション</param>
    /// <returns>LoadResultの非同期列挙</returns>
    public static async IAsyncEnumerable<LoadResult> LoadManyWithResultAsync(
        IEnumerable<string> paths,
        MjlogReaderOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        options ??= MjlogReaderOptions.Default;
        var token = options.CancellationToken != default ? options.CancellationToken : cancellationToken;

        foreach (var path in paths)
        {
            token.ThrowIfCancellationRequested();

            LoadResult result;
            try
            {
                var record = await LoadAsync(path, token);
                var validation = options.EnableValidation ? DefaultValidator.Validate(record) : null;

                result = new LoadResult
                {
                    FilePath = path,
                    Record = record,
                    Validation = validation
                };
            }
            catch (Exception ex) when (options.ContinueOnError)
            {
                result = new LoadResult
                {
                    FilePath = path,
                    Error = ex
                };
            }

            yield return result;
        }
    }

    #endregion

    #region 並列API - 複数ファイル（順序非保証・高速）

    /// <summary>
    /// 複数ファイルを並列で読み込み（順序非保証・高速）
    /// </summary>
    /// <param name="paths">ファイルパスのリスト</param>
    /// <param name="options">オプション</param>
    /// <returns>LoadResultの非同期列挙</returns>
    public static async IAsyncEnumerable<LoadResult> LoadManyParallelAsync(
        IEnumerable<string> paths,
        MjlogReaderOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        options ??= MjlogReaderOptions.Default;
        var token = options.CancellationToken != default ? options.CancellationToken : cancellationToken;

        var channel = Channel.CreateBounded<LoadResult>(new BoundedChannelOptions(options.MaxDegreeOfParallelism * 2)
        {
            SingleReader = true,
            SingleWriter = false
        });

        var pathsList = paths.ToList();

        // プロデューサータスク
        var producerTask = Task.Run(async () =>
        {
            try
            {
                await Parallel.ForEachAsync(
                    pathsList,
                    new ParallelOptions
                    {
                        MaxDegreeOfParallelism = options.MaxDegreeOfParallelism,
                        CancellationToken = token
                    },
                    async (path, ct) =>
                    {
                        LoadResult result;
                        try
                        {
                            var record = await LoadAsync(path, ct);
                            var validation = options.EnableValidation ? DefaultValidator.Validate(record) : null;

                            result = new LoadResult
                            {
                                FilePath = path,
                                Record = record,
                                Validation = validation
                            };
                        }
                        catch (Exception ex) when (options.ContinueOnError)
                        {
                            result = new LoadResult
                            {
                                FilePath = path,
                                Error = ex
                            };
                        }

                        await channel.Writer.WriteAsync(result, ct);
                    });
            }
            finally
            {
                channel.Writer.Complete();
            }
        }, token);

        // コンシューマー
        await foreach (var result in channel.Reader.ReadAllAsync(token))
        {
            yield return result;
        }

        await producerTask;
    }

    /// <summary>
    /// ディレクトリから複数ファイルを並列で読み込み（順序非保証・高速）
    /// </summary>
    /// <param name="directoryPath">ディレクトリパス</param>
    /// <param name="options">オプション</param>
    /// <returns>LoadResultの非同期列挙</returns>
    public static IAsyncEnumerable<LoadResult> LoadManyParallelAsync(
        string directoryPath,
        MjlogReaderOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        options ??= MjlogReaderOptions.Default;
        var searchOption = options.IncludeSubdirectories
            ? SearchOption.AllDirectories
            : SearchOption.TopDirectoryOnly;

        var files = Directory.GetFiles(directoryPath, options.SearchPattern, searchOption);
        return LoadManyParallelAsync(files, options, cancellationToken);
    }

    #endregion

    #region ヘルパーメソッド

    /// <summary>
    /// ファイルがGZip圧縮かどうかを判定
    /// </summary>
    private static bool IsGzipFile(string path)
    {
        var extension = Path.GetExtension(path).ToLowerInvariant();
        return extension == ".mjlog" || extension == ".gz";
    }

    #endregion
}