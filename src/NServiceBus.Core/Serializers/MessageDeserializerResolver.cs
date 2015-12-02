namespace NServiceBus.Serializers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using NServiceBus.Serialization;

    class MessageDeserializerResolver
    {
        IDictionary<string, IMessageSerializer> serializersMap;
        IMessageSerializer defaultSerializer;

        public MessageDeserializerResolver(IEnumerable<IMessageSerializer> messageSerializers, Type defaultSerializerType)
        {
            serializersMap = messageSerializers.ToDictionary(key => key.ContentType, value => value);
            defaultSerializer = serializersMap.Values.Single(serializer => serializer.GetType() == defaultSerializerType);
        }

        public IMessageSerializer Resolve(string contentType)
        {
            if (contentType == null)
            {
                //if no content type header then attempt default serializer
                return defaultSerializer;
            }
            IMessageSerializer serializer;
            if (serializersMap.TryGetValue(contentType, out serializer))
            {
                return serializer;
            }

            return defaultSerializer;
        }
    }
}