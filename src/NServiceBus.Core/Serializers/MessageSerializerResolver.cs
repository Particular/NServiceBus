namespace NServiceBus.Serializers
{
    using System.Collections.Generic;
    using System.Linq;
    using NServiceBus.Serialization;

    class MessageSerializerResolver
    {
        readonly IDictionary<string, IMessageSerializer> serializersMap;
        readonly IMessageSerializer defaultMessageSerializer;

        public MessageSerializerResolver(IEnumerable<IMessageSerializer> messageSerializers)
        {
            serializersMap = messageSerializers.ToDictionary(serializer => serializer.ContentType, serializer => serializer);
            defaultMessageSerializer = serializersMap[ContentTypes.Xml];
        }

        public IMessageSerializer Resolve(string contentType)
        {
            IMessageSerializer messageSerializer;
            if (!serializersMap.TryGetValue(contentType, out messageSerializer))
            {
                return defaultMessageSerializer;
            }

            return messageSerializer;
        }
    }
}