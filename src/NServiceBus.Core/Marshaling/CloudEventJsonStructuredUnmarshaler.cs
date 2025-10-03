namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Text.Json;
    using Transport;

    class CloudEventJsonStructuredUnmarshaler : IUnmarshalMessages
    {
        static readonly JsonSerializerOptions options = new()
        {
            PropertyNameCaseInsensitive = true
        };
        public IncomingMessage CreateIncomingMessage(MessageContext messageContext)
        {
            JsonDocument receivedCloudEvent = DeserializeOrThrow(messageContext);
            var headers = ExtractHeaders(receivedCloudEvent);
            var body = ExtractBody(receivedCloudEvent);
            string id = ExtractId(receivedCloudEvent);

            return new IncomingMessage(id, headers, body);
        }

        static ReadOnlyMemory<byte> ExtractBody(JsonDocument receivedCloudEvent)
        {
            if (receivedCloudEvent.RootElement.TryGetProperty("data_base64", out _))
            {
                return ExtractBodyFromBase64(receivedCloudEvent);
            }
            else
            {
                return ExtractBodyFromProperty(receivedCloudEvent);
            }
        }

        static ReadOnlyMemory<byte> ExtractBodyFromProperty(JsonDocument receivedCloudEvent)
        {
            if (receivedCloudEvent.RootElement.TryGetProperty("datacontenttype", out var property)
                && property.GetString()!.EndsWith("json"))
            {
                return new ReadOnlyMemory<byte>(
                    Encoding.UTF8.GetBytes(receivedCloudEvent.RootElement.GetProperty("data").GetRawText()));
            }
            else 
            {
                return new ReadOnlyMemory<byte>(
                    Encoding.UTF8.GetBytes(receivedCloudEvent.RootElement.GetProperty("data").GetString()!));
            }
        }

        static ReadOnlyMemory<byte> ExtractBodyFromBase64(JsonDocument receivedCloudEvent)
        {
            return new ReadOnlyMemory<byte>(Convert.FromBase64String(receivedCloudEvent.RootElement.GetProperty("data_base64").GetString()!));
        }

        static Dictionary<string, string> ExtractHeaders(JsonDocument receivedCloudEvent)
        {
            var propertiesToIgnore = new[] { "data", "data_base64" };

            return receivedCloudEvent
                .RootElement
                .EnumerateObject()
                .Where(p => !propertiesToIgnore.Contains(p.Name))
                .Where(p => p.Value.ValueKind != JsonValueKind.Null)
                .ToDictionary(p => p.Name, p => p.Value.GetString());
        }

        static string ExtractId(JsonDocument receivedCloudEvent) => receivedCloudEvent.RootElement.GetProperty("id").GetString();

        static void ThrowIfInvalidCloudEvent(JsonDocument receivedCloudEvent)
        {
            if (receivedCloudEvent == null)
            {
                // TODO throw
            }

            // TODO check headers throw
        }

        JsonDocument DeserializeOrThrow(MessageContext messageContext)
        {
            ThrowIfInvalidMessage(messageContext);
            var receivedCloudEvent = JsonSerializer.Deserialize<JsonDocument>(messageContext.Body.Span, options);

            ThrowIfInvalidCloudEvent(receivedCloudEvent);

            return receivedCloudEvent;
        }

        void ThrowIfInvalidMessage(MessageContext messageContext)
        {
            if (!IsValidMessage(messageContext))
            {
                // TODO throw
            }
        }

        public bool IsValidMessage(MessageContext messageContext) =>
            messageContext.Headers.TryGetValue(Headers.ContentType, out string value) && value == "application/cloudevents+json";
    }
}
