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

using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using MjlogJ.Models;

namespace MjlogJ.Formatters;

/// <summary>
/// JSON形式で出力するフォーマッタ
/// </summary>
public class JsonOutputFormatter : IOutputFormatter
{
    private readonly JsonSerializerOptions _deserializeOptions;
    private readonly JsonSerializerOptions _serializeOptions;

    public JsonOutputFormatter()
    {
        _serializeOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            Converters =
            {
                new JsonStringEnumConverter(JsonNamingPolicy.CamelCase),
                new PlayerActionWriteConverter()
            }
        };

        _deserializeOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            Converters =
            {
                new JsonStringEnumConverter(JsonNamingPolicy.CamelCase),
                new PlayerActionReadConverter()
            }
        };
    }

    public string FileExtension => ".json";

    public void Format(GameRecord record, Stream output)
    {
        JsonSerializer.Serialize(output, record, _serializeOptions);
    }

    public string FormatToString(GameRecord record)
    {
        return JsonSerializer.Serialize(record, _serializeOptions);
    }

    public GameRecord LoadFromString(string content)
    {
        return JsonSerializer.Deserialize<GameRecord>(content, _deserializeOptions)
               ?? throw new JsonException("Failed to deserialize GameRecord");
    }

    public GameRecord LoadFromStream(Stream input)
    {
        return JsonSerializer.Deserialize<GameRecord>(input, _deserializeOptions)
               ?? throw new JsonException("Failed to deserialize GameRecord");
    }
}

/// <summary>
/// PlayerActionのシリアライズ用コンバーター
/// </summary>
public class PlayerActionWriteConverter : JsonConverter<PlayerAction>
{
    public override PlayerAction Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        throw new NotSupportedException("Use PlayerActionReadConverter for deserialization");
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

                // 打牌後の手牌
                writer.WritePropertyName("handAfterDiscard");
                writer.WriteStartArray();
                foreach (var tile in discard.HandAfterDiscard)
                {
                    WriteTile(writer, tile, options);
                }

                writer.WriteEndArray();

                // 打牌後の副露
                writer.WritePropertyName("meldsAfterDiscard");
                writer.WriteStartArray();
                foreach (var meld in discard.MeldsAfterDiscard)
                {
                    JsonSerializer.Serialize(writer, meld, options);
                }

                writer.WriteEndArray();

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

    private static void WriteTile(Utf8JsonWriter writer, Tile tile, JsonSerializerOptions options)
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

/// <summary>
/// PlayerActionのデシリアライズ用コンバーター
/// </summary>
public class PlayerActionReadConverter : JsonConverter<PlayerAction>
{
    public override PlayerAction? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        if (!root.TryGetProperty("type", out var typeElement))
        {
            throw new JsonException("Missing 'type' property in PlayerAction");
        }

        var typeStr = typeElement.GetString()?.ToLowerInvariant();
        var playerId = root.TryGetProperty("playerId", out var pidElem) ? pidElem.GetInt32() : 0;
        var sequence = root.TryGetProperty("sequence", out var seqElem) ? seqElem.GetInt32() : 0;

        return typeStr switch
        {
            "draw" => new DrawAction
            {
                PlayerId = playerId,
                Sequence = sequence,
                Tile = ReadTile(root, "tile")
            },
            "discard" => new DiscardAction
            {
                PlayerId = playerId,
                Sequence = sequence,
                Tile = ReadTile(root, "tile"),
                IsTsumogiri = root.TryGetProperty("isTsumogiri", out var tsElem) && tsElem.GetBoolean(),
                HandAfterDiscard = ReadTileList(root, "handAfterDiscard"),
                MeldsAfterDiscard = ReadMeldList(root, "meldsAfterDiscard", options)
            },
            "meld" => new MeldAction
            {
                PlayerId = playerId,
                Sequence = sequence,
                Meld = ReadMeld(root, "meld", options)
            },
            "reach" => new ReachAction
            {
                PlayerId = playerId,
                Sequence = sequence,
                Step = root.TryGetProperty("step", out var stepElem) ? stepElem.GetInt32() : 1
            },
            "newdora" => new NewDoraAction
            {
                PlayerId = playerId,
                Sequence = sequence,
                DoraTile = ReadTile(root, "doraTile")
            },
            _ => throw new JsonException($"Unknown PlayerAction type: {typeStr}")
        };
    }

    public override void Write(Utf8JsonWriter writer, PlayerAction value, JsonSerializerOptions options)
    {
        throw new NotSupportedException("Use PlayerActionWriteConverter for serialization");
    }

    private static Tile? ReadTile(JsonElement parent, string propertyName)
    {
        if (!parent.TryGetProperty(propertyName, out var tileElem))
            return null;

        var suitStr = tileElem.TryGetProperty("suit", out var suitElem) ? suitElem.GetString() : "man";
        var suit = suitStr?.ToLowerInvariant() switch
        {
            "man" => TileSuit.Man,
            "pin" => TileSuit.Pin,
            "sou" => TileSuit.Sou,
            "honor" => TileSuit.Honor,
            _ => TileSuit.Man
        };

        var number = tileElem.TryGetProperty("number", out var numElem) ? numElem.GetInt32() : 1;
        var isRedDora = tileElem.TryGetProperty("isRedDora", out var redElem) && redElem.GetBoolean();
        var originalId = tileElem.TryGetProperty("originalId", out var idElem) ? idElem.GetInt32() : 0;

        return new Tile(suit, number, isRedDora, originalId);
    }

    private static List<Tile> ReadTileList(JsonElement parent, string propertyName)
    {
        var tiles = new List<Tile>();
        if (!parent.TryGetProperty(propertyName, out var arrayElem))
            return tiles;

        foreach (var tileElem in arrayElem.EnumerateArray())
        {
            var suitStr = tileElem.TryGetProperty("suit", out var suitE) ? suitE.GetString() : "man";
            var suit = suitStr?.ToLowerInvariant() switch
            {
                "man" => TileSuit.Man,
                "pin" => TileSuit.Pin,
                "sou" => TileSuit.Sou,
                "honor" => TileSuit.Honor,
                _ => TileSuit.Man
            };

            var number = tileElem.TryGetProperty("number", out var numE) ? numE.GetInt32() : 1;
            var isRedDora = tileElem.TryGetProperty("isRedDora", out var redE) && redE.GetBoolean();
            var originalId = tileElem.TryGetProperty("originalId", out var idE) ? idE.GetInt32() : 0;

            tiles.Add(new Tile(suit, number, isRedDora, originalId));
        }

        return tiles;
    }

    private static MeldInfo? ReadMeld(JsonElement parent, string propertyName, JsonSerializerOptions options)
    {
        if (!parent.TryGetProperty(propertyName, out var meldElem))
            return null;

        return JsonSerializer.Deserialize<MeldInfo>(meldElem.GetRawText(), options);
    }

    private static List<MeldInfo> ReadMeldList(JsonElement parent, string propertyName, JsonSerializerOptions options)
    {
        var melds = new List<MeldInfo>();
        if (!parent.TryGetProperty(propertyName, out var arrayElem))
            return melds;

        foreach (var meldElem in arrayElem.EnumerateArray())
        {
            var meld = JsonSerializer.Deserialize<MeldInfo>(meldElem.GetRawText(), options);
            if (meld != null)
                melds.Add(meld);
        }

        return melds;
    }
}