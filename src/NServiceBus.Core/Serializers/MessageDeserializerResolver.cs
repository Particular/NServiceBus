namespace NServiceBus.Serializers
{
    using System.Collections.Generic;
    using System.Linq;
    using NServiceBus.Serialization;

    class MessageDeserializerResolver
    {
        public IMessageSerializer DefaultSerializer { get; set; }

        readonly IDictionary<string, IMessageSerializer> serializersMap;

        public MessageDeserializerResolver(IEnumerable<IMessageSerializer> messageSerializers)
        {
            serializersMap = messageSerializers.ToDictionary(key => key.ContentType, value => value);
        }

        public IMessageSerializer Resolve(string contentType)
        {
            IMessageSerializer serializer;
            if (serializersMap.TryGetValue(contentType, out serializer))
            {
                return serializer;
            }

            return DefaultSerializer;
        }
    }
}