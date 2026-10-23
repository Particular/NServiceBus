#nullable enable

namespace NServiceBus;

using System;
using System.Collections.Generic;
using System.IO;
using Serialization;

// The interface between the serialization component and the sagas component: saga timeout payloads are serialized
// with the main serializer and deserialized with the serializer that matches their stored content type.
sealed class TimeoutSerializer(IMessageSerializer mainSerializer, MessageDeserializerResolver deserializerResolver)
{
    public string ContentType => mainSerializer.ContentType;

    public byte[] Serialize(object payload)
    {
        using var stream = new MemoryStream();
        mainSerializer.Serialize(payload, stream);
        return stream.ToArray();
    }

    public object Deserialize(ReadOnlyMemory<byte> payload, string contentType, Type messageType)
    {
        var serializer = deserializerResolver.Resolve(new Dictionary<string, string> { [Headers.ContentType] = contentType });
        return serializer.Deserialize(payload, [messageType])[0];
    }
}
