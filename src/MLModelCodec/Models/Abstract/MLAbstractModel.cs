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

namespace Foxtamp.MLModelCodec.Models.Abstract;

/// <summary>
/// ONNXモデルの抽象表現を提供するルートクラスです。
/// </summary>
/// <remarks>
/// <para>このクラスはONNXのModelProtoに対応し、モデルのメタデータと計算グラフを保持します。</para>
/// <para><see cref="Document"/>プロパティはモデルの説明などに利用され、エクスポート時に無毒化されます。</para>
/// </remarks>
// ReSharper disable once InconsistentNaming
public sealed class MLAbstractModel
{
    /// <summary>
    /// モデルが所属するrdns形式のドメイン名を取得します。
    /// </summary>
    /// <remarks>
    /// 例: "com.example.mymodel"
    /// </remarks>
    public string Domain { get; }

    /// <summary>
    /// モデルの作者名を取得します。
    /// </summary>
    public string Author { get; }

    /// <summary>
    /// モデルのバージョンを取得します。
    /// </summary>
    /// <remarks>
    /// 整数値で表現されるモデルバージョンです。
    /// </remarks>
    public int Version { get; }

    /// <summary>
    /// モデルの生成ツール名を取得します。
    /// </summary>
    public string ProducerName { get; }

    /// <summary>
    /// モデルの生成ツールバージョンを取得します。
    /// </summary>
    /// <remarks>
    /// セマンティックバージョン形式（例: "1.0.0"）を想定しています。
    /// </remarks>
    public string ProducerVersion { get; }

    /// <summary>
    /// モデルのドキュメントデータを取得します。
    /// </summary>
    /// <remarks>
    /// <para>モデルの説明などに利用されます。</para>
    /// <para>エクスポート時にエクスポーター側で無毒化処理が行われます。</para>
    /// </remarks>
    public string Document { get; }

    /// <summary>
    /// モデルのメタデータを取得します。
    /// </summary>
    /// <remarks>
    /// <para>キーと値のペアで表現される追加情報です。</para>
    /// </remarks>
    public Dictionary<string, string> Metadata { get; }

    /// <summary>
    /// 計算グラフ構造を取得します。
    /// </summary>
    public MLAbstractGraph Graph { get; }

    /// <summary>
    /// <see cref="MLAbstractModel"/>クラスの新しいインスタンスを初期化します。
    /// </summary>
    /// <param name="domain">モデルが所属するrdns形式のドメイン名</param>
    /// <param name="author">モデルの作者名</param>
    /// <param name="version">モデルのバージョン（整数値）</param>
    /// <param name="producerName">モデルの生成ツール名</param>
    /// <param name="producerVersion">モデルの生成ツールバージョン（セマンティックバージョン形式）</param>
    /// <param name="document">モデルのドキュメントデータ</param>
    /// <param name="metadata">モデルのメタデータ</param>
    /// <param name="graph">計算グラフ構造</param>
    public MLAbstractModel(
        string domain,
        string author,
        int version,
        string producerName,
        string producerVersion,
        string document,
        Dictionary<string, string> metadata,
        MLAbstractGraph graph)
    {
        Domain = domain;
        Author = author;
        Version = version;
        ProducerName = producerName;
        ProducerVersion = producerVersion;
        Document = document;
        Metadata = metadata;
        Graph = graph;
    }
}