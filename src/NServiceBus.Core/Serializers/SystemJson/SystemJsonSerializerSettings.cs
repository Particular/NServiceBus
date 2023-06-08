#nullable enable
namespace NServiceBus.Serializers.SystemJson
{
    using System.Text.Json;

    class SystemJsonSerializerSettings
    {
        public JsonReaderOptions ReaderOptions { get; set; }
        public JsonWriterOptions WriterOptions { get; set; }
        public JsonSerializerOptions? SerializerOptions { get; set; }
        public string ContentType { get; set; } = ContentTypes.Json;
    }
}
