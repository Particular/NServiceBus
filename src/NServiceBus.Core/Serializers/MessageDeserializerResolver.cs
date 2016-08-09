namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using Serialization;

    class MessageDeserializerResolver
    {
        public MessageDeserializerResolver(IMessageSerializer defaultSerializer, IEnumerable<IMessageSerializer> additionalDeserializers)
        {
            this.defaultSerializer = defaultSerializer;

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

        IMessageSerializer defaultSerializer;
        Dictionary<string, IMessageSerializer> serializersMap = new Dictionary<string, IMessageSerializer>();
    }
}