namespace NServiceBus.Serializers
{
    using System.Collections.Generic;
    using System.Linq;
    using NServiceBus.Serialization;
    using NServiceBus.Settings;

    class MessageDeserializerResolver
    {
        readonly IEnumerable<IMessageSerializer> configuredSerializers;

        public MessageDeserializerResolver(ReadOnlySettings settings, IEnumerable<IMessageSerializer> configuredSerializers)
        {
            this.configuredSerializers = configuredSerializers;
        }

        public IMessageSerializer Resolve(string contentType)
        {
            return configuredSerializers.First();
        }
    }
}