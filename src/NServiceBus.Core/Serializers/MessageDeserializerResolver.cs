namespace NServiceBus;

using System;
using System.Collections.Generic;

class MessageDeserializerResolver
{
    public MessageDeserializerResolver(MessageSerializerBag mainSerializer, IEnumerable<MessageSerializerBag> additionalDeserializers)
    {
        this.mainSerializer = mainSerializer;

        foreach (var additionalDeserializer in additionalDeserializers)
        {
            if (serializersMap.ContainsKey(additionalDeserializer.MessageSerializer.ContentType))
            {
                throw new Exception($"Multiple deserializers are registered for content-type '{additionalDeserializer.MessageSerializer.ContentType}'. Remove ambiguous deserializers.");
            }

            serializersMap.Add(additionalDeserializer.MessageSerializer.ContentType, additionalDeserializer);
        }
    }

    public MessageSerializerBag Resolve(Dictionary<string, string> headers)
    {
        if (headers.TryGetValue(Headers.ContentType, out var contentType))
        {
            if (contentType != null && serializersMap.TryGetValue(contentType, out var serializer))
            {
                return serializer;
            }
        }

        return mainSerializer;
    }

    readonly MessageSerializerBag mainSerializer;
    readonly Dictionary<string, MessageSerializerBag> serializersMap = [];
}