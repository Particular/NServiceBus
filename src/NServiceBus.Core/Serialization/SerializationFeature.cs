namespace NServiceBus
{
    using System.Collections.Generic;
    using System.Linq;
    using Features;
    using Logging;
    using MessageInterfaces;
    using MessageInterfaces.MessageMapper.Reflection;
    using Pipeline;
    using Serialization;
    using Unicast.Messages;

    class SerializationFeature : Feature
    {
        public SerializationFeature()
        {
            EnableByDefault();
            Defaults(s =>
            {
                s.SetDefault("AdditionalDeserializers", new List<SerializationDefinition>());
            });
        }

        protected internal sealed override void Setup(FeatureConfigurationContext context)
        {
            var mapper = new MessageMapper();
            var settings = context.Settings;
            var conventions = settings.Get<Conventions>();
            var messageTypes = settings.GetAvailableTypes().Where(conventions.IsMessageType);
            mapper.Initialize(messageTypes);

            SerializationDefinition defaultSerializerDefinition;

            if (!context.Settings.TryGet(out defaultSerializerDefinition))
            {
                defaultSerializerDefinition = new XmlSerializer();
            }

            var defaultSerializer = CreateMessageSerializer(defaultSerializerDefinition, mapper, context);

            var additionalDeserializerDefinitions = context.Settings.Get<List<SerializationDefinition>>("AdditionalDeserializers");
            var additionalDeserializers = additionalDeserializerDefinitions.Select(d => CreateMessageSerializer(d, mapper, context)).ToArray();
            var resolver = new MessageDeserializerResolver(defaultSerializer, additionalDeserializers);

            var knownMessages = context.Settings.GetAvailableTypes()
                .Where(context.Settings.Get<Conventions>().IsMessageType)
                .ToList();

            var messageMetadataRegistry = new MessageMetadataRegistry(context.Settings.Get<Conventions>());
            foreach (var msg in knownMessages)
            {
                messageMetadataRegistry.RegisterMessageType(msg);
            }

            var logicalMessageFactory = new LogicalMessageFactory(messageMetadataRegistry, mapper);
            context.Pipeline.Register(new DeserializeLogicalMessagesConnector(resolver, logicalMessageFactory, messageMetadataRegistry), "Deserializes the physical message body into logical messages");
            context.Pipeline.Register(new SerializeMessageConnector(defaultSerializer, messageMetadataRegistry), "Converts a logical message into a physical message");

            context.Container.ConfigureComponent(_ => mapper, DependencyLifecycle.SingleInstance);
            context.Container.ConfigureComponent(_ => messageMetadataRegistry, DependencyLifecycle.SingleInstance);
            context.Container.ConfigureComponent(_ => logicalMessageFactory, DependencyLifecycle.SingleInstance);

            LogFoundMessages(messageMetadataRegistry.GetAllMessages().ToList());
        }

        static IMessageSerializer CreateMessageSerializer(SerializationDefinition definition, IMessageMapper mapper, FeatureConfigurationContext context)
        {
            var serializerFactory = definition.Configure(context.Settings);
            var serializer = serializerFactory(mapper);
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
                string.Concat(messageDefinitions.Select(md => md.ToString() + "\n")));
        }

        static ILog Logger = LogManager.GetLogger<SerializationFeature>();
    }
}