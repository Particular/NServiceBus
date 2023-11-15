namespace NServiceBus;

using System;
using System.Collections.Generic;
using Serialization;

class MessageDeserializerResolver
{
    public MessageDeserializerResolver(IMessageSerializer mainSerializer, IEnumerable<IMessageSerializer> additionalDeserializers)
    {
        this.mainSerializer = mainSerializer;

        foreach (var additionalDeserializer in additionalDeserializers)
        {
            if (serializersMap.ContainsKey(additionalDeserializer.ContentType))
            {
                throw new Exception($"Multiple deserializers are registered for content-type '{additionalDeserializer.ContentType}'. Remove ambiguous deserializers.");
            }

            serializersMap.Add(additionalDeserializer.ContentType, additionalDeserializer);
        }
    }

    public IMessageSerializer Resolve(Dictionary<string, string> headers)
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

    readonly IMessageSerializer mainSerializer;
    readonly Dictionary<string, IMessageSerializer> serializersMap = [];
}