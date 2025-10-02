namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Text.Json;
    using System.Text.Json.Nodes;
    using Transport;

    class CloudEventJsonStructuredUnmarshaler : IUnmarshalMessages
    {
        static readonly JsonSerializerOptions options = new()
        {
            PropertyNameCaseInsensitive = true
        };
        public IncomingMessage CreateIncomingMessage(MessageContext messageContext)
        {
            JsonObject receivedCloudEvent = DeserializeOrThrow(messageContext);
            var headers = ExtractHeaders(receivedCloudEvent);
            var body = ExtractBody(receivedCloudEvent);
            string id = ExtractId(receivedCloudEvent);

            return new IncomingMessage(id, headers, body);
        }

        static ReadOnlyMemory<byte> ExtractBody(JsonObject receivedCloudEvent)
        {
            if (receivedCloudEvent.ContainsKey("data_base64"))
            {
                return ExtractBodyFromBase64(receivedCloudEvent);
            }
            else
            {
                return ExtractBodyFromProperty(receivedCloudEvent);
            }
        }

        static ReadOnlyMemory<byte> ExtractBodyFromProperty(JsonObject receivedCloudEvent)
        {
            if (receivedCloudEvent["datacontenttype"].GetValue<string>()!.EndsWith("json"))
            {
                return new ReadOnlyMemory<byte>(
                    Encoding.UTF8.GetBytes(JsonSerializer.Serialize(receivedCloudEvent["data"])));
            }
            else
            {
                return new ReadOnlyMemory<byte>(
                    Encoding.UTF8.GetBytes(receivedCloudEvent["data"].GetValue<string>()));
            }
        }

        static ReadOnlyMemory<byte> ExtractBodyFromBase64(JsonObject receivedCloudEvent)
        {
            return new ReadOnlyMemory<byte>(Convert.FromBase64String(receivedCloudEvent["data_base64"].GetValue<string>()));
        }

        static Dictionary<string, string> ExtractHeaders(JsonObject receivedCloudEvent)
        {
            var propertiesToIgnore = new[] { "data", "data_base64" };

            return receivedCloudEvent
                .Where(p => !propertiesToIgnore.Contains(p.Key))
                .Where(p => p.Value is not null)
                .ToDictionary(p => p.Key, p => p.Value!.GetValue<string>());
        }

        static string ExtractId(JsonObject receivedCloudEvent) => receivedCloudEvent["id"].GetValue<string>();

        static void ThrowIfInvalidCloudEvent(JsonObject receivedCloudEvent)
        {
            if (receivedCloudEvent == null)
            {
                // TODO throw
            }

            // TODO check headers throw
        }

        JsonObject DeserializeOrThrow(MessageContext messageContext)
        {
            ThrowIfInvalidMessage(messageContext);
            var receivedCloudEvent = JsonSerializer.Deserialize<JsonObject>(messageContext.Body.Span, options);

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
