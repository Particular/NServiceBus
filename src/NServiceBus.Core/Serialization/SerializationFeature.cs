namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Features;
    using Logging;
    using Serialization;
    using Settings;
    using Unicast.Messages;

    class SerializationFeature : Feature
    {
        public SerializationFeature()
        {
            EnableByDefault();
        }

        protected internal sealed override void Setup(FeatureConfigurationContext context)
        {
            var settings = context.Settings;
            var messageMetadataRegistry = settings.Get<MessageMetadataRegistry>();

            var defaultSerializerAndDefinition = settings.GetMainSerializer();

            var defaultSerializer = CreateMessageSerializer(defaultSerializerAndDefinition, settings);

            var additionalDeserializers = new List<IMessageSerializer>();
            foreach (var definitionAndSettings in context.Settings.GetAdditionalSerializers())
            {
                additionalDeserializers.Add(CreateMessageSerializer(definitionAndSettings, settings));
            }

            var resolver = new MessageDeserializerResolver(defaultSerializer, additionalDeserializers);

            context.Pipeline.Register(new DeserializeLogicalMessagesConnector(resolver, messageMetadataRegistry), "Deserializes the physical message body into logical messages");
            context.Pipeline.Register(new SerializeMessageConnector(defaultSerializer, messageMetadataRegistry), "Converts a logical message into a physical message");

            context.Container.ConfigureComponent(_ => messageMetadataRegistry, DependencyLifecycle.SingleInstance);

            LogFoundMessages(messageMetadataRegistry.GetAllMessages().ToList());
        }

        static IMessageSerializer CreateMessageSerializer(Tuple<SerializationDefinition, SettingsHolder> definitionAndSettings, ReadOnlySettings mainSettings)
        {
            var definition = definitionAndSettings.Item1;
            var deserializerSettings = definitionAndSettings.Item2;
            deserializerSettings.Merge(mainSettings);
            deserializerSettings.PreventChanges();

            var serializerFactory = definition.Configure(deserializerSettings);
            var serializer = serializerFactory();
            return serializer;
        }

        static void LogFoundMessages(IReadOnlyCollection<MessageMetadata> messageDefinitions)
        {
            if (!Logger.IsInfoEnabled)
            {
                return;
            }
            Logger.DebugFormat("Number of messages found: {0}", messageDefinitions.Count);
            if (!Logger.IsDebugEnabled)
            {
                return;
            }
            Logger.DebugFormat("Message definitions: \n {0}",
                string.Concat(messageDefinitions.Select(md => md + "\n")));
        }

        static ILog Logger = LogManager.GetLogger<SerializationFeature>();
    }
}