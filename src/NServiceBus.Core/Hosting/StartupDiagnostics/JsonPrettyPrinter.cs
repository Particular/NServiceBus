#nullable enable

namespace NServiceBus;

using System.Buffers;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

static class JsonPrettyPrinter
{
    internal static string Print(string input)
    {
        using var doc = JsonDocument.Parse(input);
        var buffer = new ArrayBufferWriter<byte>();
        using var writer = new Utf8JsonWriter(buffer, jsonWriterOptions);

        doc.RootElement.WriteTo(writer);

        writer.Flush();
        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }

    static readonly JsonWriterOptions jsonWriterOptions = new()
    {
        Indented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };
}
