namespace NServiceBus.Serializers
{
    using System;
    using System.Collections.Generic;
    using NServiceBus.Serialization;
    using NServiceBus.Settings;

    class MessageDeserializerResolver
    {
        readonly ReadOnlySettings settings;
        readonly HashSet<SerializerEntry> registeredSerializers = new HashSet<SerializerEntry>();

        public MessageDeserializerResolver(ReadOnlySettings settings, SerializerRegistry registry)
        {
            this.settings = settings;
        }

        public IMessageSerializer Resolve(string contentType)
        {
            return null;
        }

        internal void Register<T>(IMessageSerializer serializer) where T : SerializationDefinition
        {
            var entry = new SerializerEntry
            {
                DefinitionType = typeof(T),
                ContentType = serializer.ContentType,
                Serializer = serializer
            };

            registeredSerializers.Add(entry);
        }


        class SerializerEntry
        {
            public string ContentType { get; set; }
            public IMessageSerializer Serializer { get; set; }
            public Type DefinitionType { get; set; }
        }

    }
}