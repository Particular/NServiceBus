namespace NServiceBus.Serializers
{
    using System.Collections.Generic;
    using System.Linq;
    using NServiceBus.Serialization;

    class MessageDeserializerResolver
    {
        // :cl

        readonly IDictionary<string, IMessageSerializer> serializersMap; 

        public MessageDeserializerResolver(IMessageSerializer defaultSerializer, IEnumerable<IMessageSerializer> additionalDeserializers)
        {
            serializersMap = additionalDeserializers.ToDictionary(serializer => serializer.ContentType, serializer => serializer);
        }

        public IMessageSerializer Resolve(string contentType)
        {
            IMessageSerializer messageSerializer;
            if (!serializersMap.TryGetValue(contentType, out messageSerializer))
            {
                 return null;
            }

            return messageSerializer;
        }
    }
}