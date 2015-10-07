namespace NServiceBus.Features
{
    using System;
    using System.Linq;
    using NServiceBus.MessageInterfaces;
    using NServiceBus.Serialization;
    using NServiceBus.Serializers.XML;
    using NServiceBus.Settings;

    /// <summary>
    /// Used to configure xml as a message serializer.
    /// </summary>
    public class XmlSerialization : ConfigureSerialization
    {
        internal XmlSerialization() 
        {
            RegisterStartupTask<MessageTypesInitializer>();
        }

        /// <summary>
        /// Specify the concrete implementation of <see cref="IMessageSerializer"/> type.
        /// </summary>
        protected override Type GetSerializerType(FeatureConfigurationContext context)
        {
            return typeof(XmlMessageSerializer);
        }

        /// <summary>
        /// Initializes the mapper and the serializer with the found message types
        /// </summary>
        class MessageTypesInitializer : FeatureStartupTask
        {
            public IMessageMapper Mapper { get; set; }
            public XmlMessageSerializer Serializer { get; set; }
            public ReadOnlySettings Settings { get; set; }

            protected override void OnStart(ISendOnlyBus sendOnlyBus)
            {
                if (Mapper == null)
                {
                    return;
                }

                var messageTypes = Settings.GetAvailableTypes().Where(Settings.Get<Conventions>().IsMessageType).ToList();

                Mapper.Initialize(messageTypes);
                Serializer.Initialize(messageTypes);
            }
        }
    }
}