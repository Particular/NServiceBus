namespace NServiceBus;

using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

static partial class HeaderSerializer
{
    public static string Serialize(Dictionary<string, string> dictionary) =>
        JsonSerializer.Serialize(dictionary, HeaderSerializationContext.Default.DictionaryStringString);

    public static Dictionary<string, string> Deserialize(string value) =>
        JsonSerializer.Deserialize(value, HeaderSerializationContext.Default.DictionaryStringString);

    [JsonSourceGenerationOptions(WriteIndented = true, IndentSize = 2)]
    [JsonSerializable(typeof(Dictionary<string, string>))]
    sealed partial class HeaderSerializationContext : JsonSerializerContext;
}