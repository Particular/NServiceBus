namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using NServiceBus.Serialization;

    class MessageDeserializerResolver
    {
        Dictionary<string, IMessageSerializer> serializersMap;
        IMessageSerializer defaultSerializer;

        public MessageDeserializerResolver(IEnumerable<IMessageSerializer> messageSerializers, Type defaultSerializerType)
        {
            serializersMap = messageSerializers.ToDictionary(key => key.ContentType, value => value);
            defaultSerializer = serializersMap.Values.Single(serializer => serializer.GetType() == defaultSerializerType);
        }

        public IMessageSerializer Resolve(Dictionary<string, string> headers)
        {
            string contentType;
            if (headers.TryGetValue(Headers.ContentType, out contentType))
            {
                IMessageSerializer serializer;
                if (serializersMap.TryGetValue(contentType, out serializer))
                {
                    return serializer;
                }
            }
            return defaultSerializer;
        }
    }
}