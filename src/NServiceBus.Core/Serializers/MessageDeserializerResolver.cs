namespace NServiceBus.Serializers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using NServiceBus.Serialization;

    class MessageDeserializerResolver
    {
        public Type DefaultSerializerType { get; set; }
        IMessageSerializer defaultSerializer;
        readonly IDictionary<string, IMessageSerializer> serializersMap;

        public MessageDeserializerResolver(IEnumerable<IMessageSerializer> messageSerializers)
        {
            serializersMap = messageSerializers.ToDictionary(key => key.ContentType, value => value);
        }

        public IMessageSerializer Resolve(string contentType)
        {
            if (DefaultSerializerType != null)
            {
                defaultSerializer = serializersMap.Values.First(s => s.GetType() == DefaultSerializerType);
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