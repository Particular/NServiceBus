namespace NServiceBus;

using System.Text.Encodings.Web;
using System.Text.Json;

static class JsonPrettyPrinter
{
    internal static string Print(string input)
    {
        using var doc = JsonDocument.Parse(input);
        var root = doc.RootElement;

        return JsonSerializer.Serialize(root, jsonSerializerOptions);
    }

    static readonly JsonSerializerOptions jsonSerializerOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };
}