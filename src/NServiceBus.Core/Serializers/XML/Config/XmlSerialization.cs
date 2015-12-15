namespace NServiceBus.Features
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
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
        }

        /// <summary>
        /// Specify the concrete implementation of <see cref="IMessageSerializer"/> type.
        /// </summary>
        protected override Type GetSerializerType(FeatureConfigurationContext context)
        {
            context.Container.ConfigureComponent<MessageTypesInitializer>(DependencyLifecycle.SingleInstance);

            context.RegisterStartupTask(b => b.Build<MessageTypesInitializer>());
            return typeof(XmlMessageSerializer);
        }

        /// <summary>
        /// Initializes the mapper and the serializer with the found message types
        /// </summary>
        class MessageTypesInitializer : FeatureStartupTask
        {
            public XmlMessageSerializer Serializer { get; set; }
            public ReadOnlySettings Settings { get; set; }

            protected override Task OnStart(IBusSession session)
            {
                var conventions = Settings.Get<Conventions>();
                var messageTypes = Settings.GetAvailableTypes()
                    .Where(conventions.IsMessageType).ToList();
                Serializer.Initialize(messageTypes);
                return TaskEx.Completed;
            }

            protected override Task OnStop(IBusSession session)
            {
                return TaskEx.Completed;
            }
        }
    }
}