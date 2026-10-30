#nullable enable

namespace NServiceBus;

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Serialization;

// Deserializes the control messages ServiceControl sends to an endpoint's control queue. It's registered next to the
// endpoint's own deserializers under a content type of its own, so control messages are deserialized the same way
// whichever serializer the endpoint uses.
sealed class ControlMessageSerializer : IMessageSerializer
{
    public const string ControlContentType = "application/vnd.particular.control+json";

    public string ContentType => ControlContentType;

    public void Serialize(object message, Stream stream) =>
        throw new NotSupportedException("Control messages are only received by endpoints, never sent.");

    public object[] Deserialize(ReadOnlyMemory<byte> body, IList<Type>? messageTypes = null)
    {
        if (messageTypes is [var messageType, ..] && messageType == typeof(SagaTimeoutChangeRequest))
        {
            var request = JsonSerializer.Deserialize(body.Span, ControlMessagesJsonContext.Default.SagaTimeoutChangeRequest)
                          ?? throw new InvalidSagaTimeoutManagementRequestException("The saga timeout change request is empty.");
            request.Validate();
            return [request];
        }

        throw new InvalidOperationException($"'{messageTypes?[0].FullName}' isn't a known control message type.");
    }
}

[JsonSerializable(typeof(SagaTimeoutChangeRequest))]
sealed partial class ControlMessagesJsonContext : JsonSerializerContext;
