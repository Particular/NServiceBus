#nullable enable

namespace NServiceBus;

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

static partial class HeaderSerializer
{
    public static string Serialize(Dictionary<string, string> dictionary) =>
        JsonSerializer.Serialize(dictionary, HeaderSerializationContext.Default.DictionaryStringString);

    public static Dictionary<string, string> Deserialize(string value) =>
        Deserialize(Encoding.UTF8.GetBytes(value), pool: null);

    /// <summary>
    /// Deserializes header JSON (UTF-8) into a dictionary, renting from
    /// <paramref name="pool"/> when provided to avoid allocating a fresh dictionary.
    /// When <paramref name="pool"/> is null a new dictionary is allocated.
    /// </summary>
    /// <remarks>
    /// Uses a manual <see cref="Utf8JsonReader"/> parse rather than
    /// <c>JsonSerializer.Deserialize&lt;Dictionary&lt;string,string&gt;&gt;</c>, because
    /// System.Text.Json always allocates a fresh dictionary and cannot populate a
    /// rented/pooled one, so the pool would cut zero GC pressure otherwise.
    /// </remarks>
    public static Dictionary<string, string> Deserialize(ReadOnlySpan<byte> utf8Json, DictionaryPool<string, string>? pool = null)
    {
        var dict = pool?.Rent() ?? [];
        try
        {
            Populate(utf8Json, dict);
        }
        catch
        {
            // Return the rented dictionary (which clears it) so a parse failure
            // doesn't waste the pooled instance.
            pool?.Return(dict);
            throw;
        }

        return dict;
    }

    static void Populate(ReadOnlySpan<byte> utf8Json, Dictionary<string, string> dict)
    {
        var reader = new Utf8JsonReader(utf8Json);

        if (!reader.Read())
        {
            throw new JsonException("Invalid JSON: no data.");
        }

        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException($"Invalid JSON: expected start of object, got {reader.TokenType}.");
        }

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                return;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException($"Invalid JSON: expected property name, got {reader.TokenType}.");
            }

            var key = reader.GetString()!;

            if (!reader.Read())
            {
                throw new JsonException("Invalid JSON: expected value.");
            }

            if (reader.TokenType == JsonTokenType.String)
            {
                dict[key] = reader.GetString()!;
            }
            else if (reader.TokenType == JsonTokenType.Null)
            {
                dict[key] = null!;
            }
            else
            {
                throw new JsonException($"Invalid JSON: expected string value, got {reader.TokenType}.");
            }
        }

        throw new JsonException("Invalid JSON: unexpected end of input.");
    }

    [JsonSourceGenerationOptions(WriteIndented = true, IndentSize = 2, NewLine = "\r\n")]
    [JsonSerializable(typeof(Dictionary<string, string>))]
    internal sealed partial class HeaderSerializationContext : JsonSerializerContext;
}