#nullable enable
namespace NServiceBus.Serializers.SystemJson;

using System.Text.Json;

class SystemJsonSerializerSettings
{
    public JsonSerializerOptions? SerializerOptions { get; set; }
    public string ContentType { get; set; } = ContentTypes.Json;
}