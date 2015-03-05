namespace NServiceBus.Features
{
    using System.Linq;
    using NServiceBus.MessageInterfaces.MessageMapper.Reflection;
    using NServiceBus.ObjectBuilder;
    using NServiceBus.Serializers.XML;

    /// <summary>
    /// Used to configure xml as a message serializer
    /// </summary>
    public class XmlSerialization : Feature
    {
        internal XmlSerialization()
        {
            EnableByDefault();
            Prerequisite(this.ShouldSerializationFeatureBeEnabled, "XmlSerialization not enable since serialization definition not detected.");
            RegisterStartupTask<MessageTypesInitializer>();
        }

        /// <inheritdoc />
        protected internal override void Setup(FeatureConfigurationContext context)
        {
            context.Container.ConfigureComponent<MessageMapper>(DependencyLifecycle.SingleInstance);
            var c = context.Container.ConfigureComponent<XmlMessageSerializer>(DependencyLifecycle.SingleInstance);

            context.Settings.ApplyTo<XmlMessageSerializer>((IComponentConfig)c);
        }

        /// <summary>
        /// Initializes the mapper and the serializer with the found message types
        /// </summary>
        class MessageTypesInitializer : FeatureStartupTask
        {
            public MessageMapper Mapper { get; set; }
            public XmlMessageSerializer Serializer { get; set; }
            public Configure Config { get; set; }

            protected override void OnStart()
            {
                if (Mapper == null)
                {
                    return;
                }

                var messageTypes = Config.TypesToScan.Where(Config.Settings.Get<Conventions>().IsMessageType).ToList();

                Mapper.Initialize(messageTypes);
                Serializer.Initialize(messageTypes);
            }
        }

    }
}