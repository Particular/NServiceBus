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
        const string TYPE_PROPERTY = "type";
        const string DATA_CONTENT_TYPE_PROPERTY = "datacontenttype";
        const string DATA_PROPERTY = "data";
        const string DATA_BASE64_PROPERTY = "data_base64";
        const string ID_PROPERTY = "id";
        const string JSON_SUFFIX = "json";
        const string SUPPORTED_CONTENT_TYPE = "application/cloudevents+json";

        static readonly JsonSerializerOptions options = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public IncomingMessage CreateIncomingMessage(MessageContext messageContext)
        {
            JsonDocument receivedCloudEvent = DeserializeOrThrow(messageContext);
            var headers = ExtractHeaders(messageContext.Headers, receivedCloudEvent);
            var body = ExtractBody(receivedCloudEvent);

            return new IncomingMessage(messageContext.NativeMessageId, headers, body);
        }

        static ReadOnlyMemory<byte> ExtractBody(JsonDocument receivedCloudEvent)
        {
            if (receivedCloudEvent.RootElement.TryGetProperty(DATA_BASE64_PROPERTY, out _))
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
            if (receivedCloudEvent.RootElement.TryGetProperty(DATA_CONTENT_TYPE_PROPERTY, out var property)
                && property.GetString()!.EndsWith(JSON_SUFFIX))
            {
                return new ReadOnlyMemory<byte>(
                    Encoding.UTF8.GetBytes(receivedCloudEvent.RootElement.GetProperty(DATA_PROPERTY).GetRawText()));
            }

            return new ReadOnlyMemory<byte>(
                    Encoding.UTF8.GetBytes(receivedCloudEvent.RootElement.GetProperty(DATA_PROPERTY).GetString()!));
        }

        static ReadOnlyMemory<byte> ExtractBodyFromBase64(JsonDocument receivedCloudEvent)
        {
            return new ReadOnlyMemory<byte>(Convert.FromBase64String(receivedCloudEvent.RootElement.GetProperty(DATA_BASE64_PROPERTY).GetString()!));
        }

        static Dictionary<string, string> ExtractHeaders(Dictionary<string, string> existingHeaders, JsonDocument receivedCloudEvent)
        {
            var propertiesToIgnore = new[] { DATA_PROPERTY, DATA_BASE64_PROPERTY };

            var id = ExtractId(receivedCloudEvent);

            var headersCopy = existingHeaders.ToDictionary(k => k.Key, k => k.Value);

            foreach (var kvp in receivedCloudEvent
                         .RootElement
                         .EnumerateObject()
                         .Where(p => !propertiesToIgnore.Contains(p.Name))
                         .Where(p => p.Value.ValueKind != JsonValueKind.Null))
            {
                headersCopy[kvp.Name] = kvp.Value.ValueKind == JsonValueKind.String
                    ? kvp.Value.GetString()!
                    : kvp.Value.GetRawText();
            }

            headersCopy[Headers.MessageId] = id;

            return headersCopy;
        }

        static string ExtractId(JsonDocument receivedCloudEvent) => receivedCloudEvent.RootElement.GetProperty(ID_PROPERTY).GetString();

        static void ThrowIfInvalidCloudEvent(JsonDocument receivedCloudEvent)
        {
            if (receivedCloudEvent == null)
            {
                throw new NotSupportedException("Couldn't deserialize the message into a cloud event");
            }

            foreach (var property in new string[] { DATA_CONTENT_TYPE_PROPERTY, ID_PROPERTY, TYPE_PROPERTY })
            {
                if (!receivedCloudEvent.RootElement.TryGetProperty(property, out _))
                {
                    throw new NotSupportedException($"Message lacks {property} property");
                }
            }

            if (!receivedCloudEvent.RootElement.TryGetProperty(DATA_BASE64_PROPERTY, out _) &&
                !receivedCloudEvent.RootElement.TryGetProperty(DATA_PROPERTY, out _))
            {
                throw new NotSupportedException($"Message lacks both {DATA_PROPERTY} and {DATA_BASE64_PROPERTY} property");
            }
        }

        static JsonDocument DeserializeOrThrow(MessageContext messageContext)
        {
            ThrowIfInvalidMessage(messageContext);
            var receivedCloudEvent = JsonSerializer.Deserialize<JsonDocument>(messageContext.Body.Span, options);

            ThrowIfInvalidCloudEvent(receivedCloudEvent);

            return receivedCloudEvent;
        }

        static void ThrowIfInvalidMessage(MessageContext messageContext)
        {

            if (messageContext.Headers.TryGetValue(Headers.ContentType, out string value))
            {
                if (value != SUPPORTED_CONTENT_TYPE)
                {
                    throw new NotSupportedException($"Unsupported content type {value}");
                }
            }
            else
            {
                throw new NotSupportedException("Missing content type");
            }
        }

        public bool IsValidMessage(MessageContext messageContext) =>
            messageContext.Headers.TryGetValue(Headers.ContentType, out string value) && value == SUPPORTED_CONTENT_TYPE;
    }
}
