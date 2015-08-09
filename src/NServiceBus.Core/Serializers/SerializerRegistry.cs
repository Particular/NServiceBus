namespace NServiceBus.Serializers
{
    using System.Collections.Generic;
    using System.Linq;
    using NServiceBus.Serialization;
    using NServiceBus.Settings;

    class SerializerRegistry
    {
        readonly ReadOnlySettings settings;


        public SerializerRegistry(ReadOnlySettings settings)
        {
            this.settings = settings;
        }


        public MessageDeserializerResolver CreateResolver()
        {
            

            return new MessageDeserializerResolver(settings, this);
        }

    }
}