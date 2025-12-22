// zlib License
// 
// Copyright (c) 2025 Sinoa
// 
// This software is provided ‘as-is’, without any express or implied
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

using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using MjlogConverter.Models;

namespace MjlogConverter.Formatters;

/// <summary>
/// JSON形式で出力するフォーマッタ
/// </summary>
public class JsonOutputFormatter : IOutputFormatter
{
    private readonly JsonSerializerOptions _options;

    public string FileExtension => ".json";

    public JsonOutputFormatter()
    {
        _options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            Converters =
            {
                new JsonStringEnumConverter(JsonNamingPolicy.CamelCase),
                new PlayerActionConverter()
            }
        };
    }

    public void Format(GameRecord record, Stream output)
    {
        JsonSerializer.Serialize(output, record, _options);
    }

    public string FormatToString(GameRecord record)
    {
        return JsonSerializer.Serialize(record, _options);
    }
}

/// <summary>
/// PlayerActionのポリモーフィックシリアライズ用コンバーター
/// </summary>
public class PlayerActionConverter : JsonConverter<PlayerAction>
{
    public override PlayerAction Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // デシリアライズは今回は非対応
        throw new NotSupportedException("Deserialization of PlayerAction is not supported");
    }

    public override void Write(Utf8JsonWriter writer, PlayerAction value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        // 共通プロパティ
        writer.WriteString("type", value.Type.ToString().ToLowerInvariant());
        writer.WriteNumber("playerId", value.PlayerId);
        writer.WriteNumber("sequence", value.Sequence);

        // 派生クラス固有のプロパティ
        switch (value)
        {
            case DrawAction draw:
                if (draw.Tile != null)
                {
                    writer.WritePropertyName("tile");
                    WriteTile(writer, draw.Tile, options);
                }

                break;

            case DiscardAction discard:
                if (discard.Tile != null)
                {
                    writer.WritePropertyName("tile");
                    WriteTile(writer, discard.Tile, options);
                }

                writer.WriteBoolean("isTsumogiri", discard.IsTsumogiri);
                break;

            case MeldAction meld:
                if (meld.Meld != null)
                {
                    writer.WritePropertyName("meld");
                    JsonSerializer.Serialize(writer, meld.Meld, options);
                }

                break;

            case ReachAction reach:
                writer.WriteNumber("step", reach.Step);
                break;

            case NewDoraAction newDora:
                if (newDora.DoraTile != null)
                {
                    writer.WritePropertyName("doraTile");
                    WriteTile(writer, newDora.DoraTile, options);
                }

                break;
        }

        writer.WriteEndObject();
    }

    private void WriteTile(Utf8JsonWriter writer, Tile tile, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString("suit", tile.Suit.ToString().ToLowerInvariant());
        writer.WriteNumber("number", tile.Number);
        writer.WriteBoolean("isRedDora", tile.IsRedDora);
        writer.WriteNumber("originalId", tile.OriginalId);
        writer.WriteString("displayName", tile.DisplayName);
        writer.WriteEndObject();
    }
}