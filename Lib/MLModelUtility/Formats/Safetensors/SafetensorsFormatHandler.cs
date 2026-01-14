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

using System.Buffers.Binary;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using MLModelUtility.Capabilities;
using MLModelUtility.IO;
using MLModelUtility.Models;

namespace MLModelUtility.Formats.Safetensors;

/// <summary>
/// Safetensorsフォーマットのハンドラ
/// </summary>
public class SafetensorsFormatHandler : IModelFormatHandler, ITensorReader, ITensorWriter
{
    private const string MetadataKey = "__metadata__";

    /// <inheritdoc />
    public string FormatName => "Safetensors";

    /// <inheritdoc />
    public string FileExtension => ".safetensors";

    /// <inheritdoc />
    public ModelFormatCapability Capability => ModelFormatCapability.TensorOnly;

    #region ITensorReader Implementation

    /// <inheritdoc />
    public TensorCollection ReadTensors(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        // ヘッダーサイズを読み込む（8バイト、リトルエンディアン）
        Span<byte> headerSizeBytes = stackalloc byte[8];
        var bytesRead = stream.ReadAtLeast(headerSizeBytes, 8, throwOnEndOfStream: true);
        var headerSize = BinaryPrimitives.ReadInt64LittleEndian(headerSizeBytes);

        if (headerSize <= 0 || headerSize > int.MaxValue)
        {
            throw new InvalidDataException($"Invalid header size: {headerSize}");
        }

        // JSONヘッダーを読み込む
        var headerBytes = new byte[headerSize];
        stream.ReadExactly(headerBytes);
        var headerJson = Encoding.UTF8.GetString(headerBytes);

        // ヘッダーをパース
        var (tensorHeaders, metadata) = ParseHeader(headerJson);

        // バイナリデータの開始位置
        var dataOffset = 8 + headerSize;

        // テンソルデータを読み込む
        var tensors = new List<ITensorData>();
        foreach (var (name, header) in tensorHeaders)
        {
            var dataType = TensorDataTypeExtensions.FromSafetensorsTypeName(header.DataType);
            var info = new TensorInfo(name, header.Shape, dataType);

            var start = header.DataOffsets[0];
            var end = header.DataOffsets[1];
            var length = (int)(end - start);

            // ストリーム位置を設定してデータを読み込む
            stream.Position = dataOffset + start;
            var data = new byte[length];
            stream.ReadExactly(data);

            tensors.Add(new TensorData(info, data));
        }

        return new TensorCollection(tensors, metadata);
    }

    /// <inheritdoc />
    public async Task<TensorCollection> ReadTensorsAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);

        // ヘッダーサイズを読み込む（8バイト、リトルエンディアン）
        var headerSizeBytes = new byte[8];
        await stream.ReadExactlyAsync(headerSizeBytes, cancellationToken);
        var headerSize = BinaryPrimitives.ReadInt64LittleEndian(headerSizeBytes);

        if (headerSize <= 0 || headerSize > int.MaxValue)
        {
            throw new InvalidDataException($"Invalid header size: {headerSize}");
        }

        // JSONヘッダーを読み込む
        var headerBytes = new byte[headerSize];
        await stream.ReadExactlyAsync(headerBytes, cancellationToken);
        var headerJson = Encoding.UTF8.GetString(headerBytes);

        // ヘッダーをパース
        var (tensorHeaders, metadata) = ParseHeader(headerJson);

        // バイナリデータの開始位置
        var dataOffset = 8 + headerSize;

        // テンソルデータを読み込む
        var tensors = new List<ITensorData>();
        foreach (var (name, header) in tensorHeaders)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var dataType = TensorDataTypeExtensions.FromSafetensorsTypeName(header.DataType);
            var info = new TensorInfo(name, header.Shape, dataType);

            var start = header.DataOffsets[0];
            var end = header.DataOffsets[1];
            var length = (int)(end - start);

            // ストリーム位置を設定してデータを読み込む
            stream.Position = dataOffset + start;
            var data = new byte[length];
            await stream.ReadExactlyAsync(data, cancellationToken);

            tensors.Add(new TensorData(info, data));
        }

        return new TensorCollection(tensors, metadata);
    }

    /// <inheritdoc />
    public TensorCollection ReadTensorsFromFile(string filePath)
    {
        using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        return ReadTensors(stream);
    }

    /// <inheritdoc />
    public async Task<TensorCollection> ReadTensorsFromFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        await using var stream = new FileStream(
            filePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 4096,
            useAsync: true);
        return await ReadTensorsAsync(stream, cancellationToken);
    }

    #endregion

    #region ITensorWriter Implementation

    /// <inheritdoc />
    public void WriteTensors(TensorCollection tensors, Stream stream)
    {
        ArgumentNullException.ThrowIfNull(tensors);
        ArgumentNullException.ThrowIfNull(stream);

        // ヘッダーを構築
        var (headerJson, tensorOrder) = BuildHeader(tensors);
        var headerBytes = Encoding.UTF8.GetBytes(headerJson);

        // ヘッダーサイズを書き込む
        Span<byte> headerSizeBytes = stackalloc byte[8];
        BinaryPrimitives.WriteInt64LittleEndian(headerSizeBytes, headerBytes.Length);
        stream.Write(headerSizeBytes);

        // JSONヘッダーを書き込む
        stream.Write(headerBytes);

        // テンソルデータを書き込む（オフセット順）
        foreach (var tensor in tensorOrder)
        {
            var data = tensor.GetDataMemory();
            stream.Write(data.Span);
        }
    }

    /// <inheritdoc />
    public async Task WriteTensorsAsync(TensorCollection tensors, Stream stream, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(tensors);
        ArgumentNullException.ThrowIfNull(stream);

        // ヘッダーを構築
        var (headerJson, tensorOrder) = BuildHeader(tensors);
        var headerBytes = Encoding.UTF8.GetBytes(headerJson);

        // ヘッダーサイズを書き込む
        var headerSizeBytes = new byte[8];
        BinaryPrimitives.WriteInt64LittleEndian(headerSizeBytes, headerBytes.Length);
        await stream.WriteAsync(headerSizeBytes, cancellationToken);

        // JSONヘッダーを書き込む
        await stream.WriteAsync(headerBytes, cancellationToken);

        // テンソルデータを書き込む（オフセット順）
        foreach (var tensor in tensorOrder)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var data = tensor.GetDataMemory();
            await stream.WriteAsync(data, cancellationToken);
        }
    }

    /// <inheritdoc />
    public void WriteTensorsToFile(TensorCollection tensors, string filePath)
    {
        using var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
        WriteTensors(tensors, stream);
    }

    /// <inheritdoc />
    public async Task WriteTensorsToFileAsync(TensorCollection tensors, string filePath, CancellationToken cancellationToken = default)
    {
        await using var stream = new FileStream(
            filePath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 4096,
            useAsync: true);
        await WriteTensorsAsync(tensors, stream, cancellationToken);
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// JSONヘッダーをパースしてテンソルヘッダーとメタデータを取得
    /// </summary>
    private static (Dictionary<string, SafetensorsTensorHeader> Tensors, Dictionary<string, string>? Metadata) ParseHeader(string headerJson)
    {
        var jsonNode = JsonNode.Parse(headerJson)
                       ?? throw new InvalidDataException("Failed to parse header JSON");

        var jsonObject = jsonNode.AsObject();
        var tensors = new Dictionary<string, SafetensorsTensorHeader>();
        Dictionary<string, string>? metadata = null;

        foreach (var (key, value) in jsonObject)
        {
            if (key == MetadataKey)
            {
                // メタデータを処理
                if (value is JsonObject metaObj)
                {
                    metadata = new Dictionary<string, string>();
                    foreach (var (metaKey, metaValue) in metaObj)
                    {
                        metadata[metaKey] = metaValue?.ToString() ?? "";
                    }
                }
            }
            else
            {
                // テンソルヘッダーを処理
                var header = value.Deserialize<SafetensorsTensorHeader>()
                             ?? throw new InvalidDataException($"Failed to parse tensor header for '{key}'");
                tensors[key] = header;
            }
        }

        return (tensors, metadata);
    }

    /// <summary>
    /// テンソルコレクションからJSONヘッダーを構築
    /// </summary>
    private static (string HeaderJson, List<ITensorData> TensorOrder) BuildHeader(TensorCollection tensors)
    {
        var jsonObject = new JsonObject();
        var tensorOrder = new List<ITensorData>();
        long currentOffset = 0;

        // メタデータを追加
        if (tensors.Metadata != null && tensors.Metadata.Count > 0)
        {
            var metadataObject = new JsonObject();
            foreach (var (key, value) in tensors.Metadata)
            {
                metadataObject[key] = value;
            }

            jsonObject[MetadataKey] = metadataObject;
        }

        // テンソルを追加（名前でソート）
        var sortedTensors = tensors.OrderBy(t => t.Info.Name).ToList();
        foreach (var tensor in sortedTensors)
        {
            var info = tensor.Info;
            var byteSize = info.ByteSize;

            var tensorHeader = new JsonObject
            {
                ["dtype"] = info.DataType.ToSafetensorsTypeName(),
                ["shape"] = new JsonArray(info.Shape.Select(s => JsonValue.Create(s)).ToArray()),
                ["data_offsets"] = new JsonArray(
                    JsonValue.Create(currentOffset),
                    JsonValue.Create(currentOffset + byteSize))
            };

            jsonObject[info.Name] = tensorHeader;
            tensorOrder.Add(tensor);
            currentOffset += byteSize;
        }

        var options = new JsonSerializerOptions
        {
            WriteIndented = false
        };
        var headerJson = jsonObject.ToJsonString(options);

        return (headerJson, tensorOrder);
    }

    #endregion
}