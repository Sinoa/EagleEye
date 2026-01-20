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

using System.IO.Compression;
using Foxtamp.MjlogReader.Models;
using Foxtamp.MjlogReader.Parsers;

namespace Foxtamp.MjlogReader;

/// <summary>
/// 天鳳牌譜（mjlog）を読み込むためのファサードAPI
/// </summary>
public static class MjlogDocumentReader
{
    private static readonly MjlogXmlParser DefaultParser = new();

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

    #region 同期API - ファイルパス

    /// <summary>
    /// ファイルから牌譜を読み込み（GZip自動判定）
    /// </summary>
    /// <param name="path">ファイルパス</param>
    /// <returns>MjlogDocument</returns>
    public static MjlogDocument Load(string path)
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
    /// <returns>MjlogDocument</returns>
    // ReSharper disable once MemberCanBePrivate.Global
    public static MjlogDocument LoadFromGzip(string path)
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
    /// <returns>MjlogDocument</returns>
    // ReSharper disable once MemberCanBePrivate.Global
    public static MjlogDocument Parse(string xmlContent)
    {
        return DefaultParser.Parse(xmlContent);
    }

    #endregion

    #region 同期API - ストリーム

    /// <summary>
    /// ストリームから牌譜を読み込み（GZip圧縮なし）
    /// </summary>
    /// <param name="stream">入力ストリーム</param>
    /// <returns>MjlogDocument</returns>
    public static MjlogDocument Load(Stream stream)
    {
        using var reader = new StreamReader(stream, leaveOpen: true);
        var content = reader.ReadToEnd();
        return Parse(content);
    }

    /// <summary>
    /// GZipストリームから牌譜を読み込み
    /// </summary>
    /// <param name="gzipStream">GZip圧縮されたストリーム</param>
    /// <returns>MjlogDocument</returns>
    public static MjlogDocument LoadFromGzip(Stream gzipStream)
    {
        using var decompressStream = new GZipStream(gzipStream, CompressionMode.Decompress, leaveOpen: true);
        using var reader = new StreamReader(decompressStream);
        var content = reader.ReadToEnd();
        return Parse(content);
    }

    #endregion

    #region 非同期API - ファイルパス

    /// <summary>
    /// 非同期でファイルから牌譜を読み込み（GZip自動判定）
    /// </summary>
    /// <param name="path">ファイルパス</param>
    /// <param name="cancellationToken">キャンセルトークン</param>
    /// <returns>MjlogDocument</returns>
    public static async Task<MjlogDocument> LoadAsync(string path, CancellationToken cancellationToken = default)
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
    /// <returns>MjlogDocument</returns>
    // ReSharper disable once MemberCanBePrivate.Global
    public static async Task<MjlogDocument> LoadFromGzipAsync(string path, CancellationToken cancellationToken = default)
    {
        await using var fileStream = File.OpenRead(path);
        await using var gzipStream = new GZipStream(fileStream, CompressionMode.Decompress);
        using var reader = new StreamReader(gzipStream);
        var content = await reader.ReadToEndAsync(cancellationToken);
        return Parse(content);
    }

    #endregion

    #region 非同期API - ストリーム

    /// <summary>
    /// 非同期でストリームから牌譜を読み込み（GZip圧縮なし）
    /// </summary>
    /// <param name="stream">入力ストリーム</param>
    /// <param name="cancellationToken">キャンセルトークン</param>
    /// <returns>MjlogDocument</returns>
    public static async Task<MjlogDocument> LoadAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        using var reader = new StreamReader(stream, leaveOpen: true);
        var content = await reader.ReadToEndAsync(cancellationToken);
        return Parse(content);
    }

    /// <summary>
    /// 非同期でGZipストリームから牌譜を読み込み
    /// </summary>
    /// <param name="gzipStream">GZip圧縮されたストリーム</param>
    /// <param name="cancellationToken">キャンセルトークン</param>
    /// <returns>MjlogDocument</returns>
    public static async Task<MjlogDocument> LoadFromGzipAsync(Stream gzipStream, CancellationToken cancellationToken = default)
    {
        await using var decompressStream = new GZipStream(gzipStream, CompressionMode.Decompress, leaveOpen: true);
        using var reader = new StreamReader(decompressStream);
        var content = await reader.ReadToEndAsync(cancellationToken);
        return Parse(content);
    }

    #endregion
}