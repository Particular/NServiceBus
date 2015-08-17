namespace NServiceBus.Serializers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using NServiceBus.Serialization;

    class MessageDeserializerResolver
    {
        readonly IDictionary<string, IMessageSerializer> serializersMap;
        readonly IMessageSerializer defaultSerializer;

        public MessageDeserializerResolver(IEnumerable<IMessageSerializer> messageSerializers, Type defaultSerializerType)
        {
            Guard.AgainstNull("defaultSerializerType", defaultSerializerType);

            serializersMap = messageSerializers.ToDictionary(key => key.ContentType, value => value);
            defaultSerializer = serializersMap.Values.Single(serializer => serializer.GetType() == defaultSerializerType);
        }

        public IMessageSerializer Resolve(string contentType)
        {
            IMessageSerializer serializer;
            if (serializersMap.TryGetValue(contentType, out serializer))
            {
                return serializer;
            }

            return defaultSerializer;
        }
    }
}