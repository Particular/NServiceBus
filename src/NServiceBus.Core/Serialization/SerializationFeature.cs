namespace NServiceBus;

using System;
using System.Collections.Generic;
using System.Linq;
using Features;
using Logging;
using MessageInterfaces;
using Microsoft.Extensions.DependencyInjection;
using Pipeline;
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
        var mapper = context.Settings.Get<IMessageMapper>();
        var settings = context.Settings;
        var messageMetadataRegistry = settings.Get<MessageMetadataRegistry>();
        mapper.Initialize(messageMetadataRegistry.GetAllMessages().Select(m => m.MessageType));

        var mainSerializerAndDefinition = settings.GetMainSerializer();

        var mainSerializer = CreateMessageSerializer(mainSerializerAndDefinition, mapper, settings);

        var additionalDeserializerDefinitions = context.Settings.GetAdditionalSerializers();
        var additionalDeserializers = new List<IMessageSerializer>();

        var additionalDeserializerDiagnostics = new List<object>();
        foreach (var definitionAndSettings in additionalDeserializerDefinitions)
        {
            var deserializer = CreateMessageSerializer(definitionAndSettings, mapper, settings);
            additionalDeserializers.Add(deserializer);

            var deserializerType = definitionAndSettings.Item1.GetType();

            additionalDeserializerDiagnostics.Add(new
            {
                Type = deserializerType.FullName,
                Version = FileVersionRetriever.GetFileVersion(deserializerType),
                deserializer.ContentType
            });
        }

        var allowMessageTypeInference = settings.IsMessageTypeInferenceEnabled();
        var allowDynamicTypeLoading = settings.IsDynamicTypeLoadingEnabled();
        var resolver = new MessageDeserializerResolver(mainSerializer, additionalDeserializers);
        var logicalMessageFactory = new LogicalMessageFactory(messageMetadataRegistry, mapper);
        context.Pipeline.Register("DeserializeLogicalMessagesConnector", new DeserializeMessageConnector(resolver, logicalMessageFactory, messageMetadataRegistry, mapper, allowMessageTypeInference), "Deserializes the physical message body into logical messages");
        context.Pipeline.Register("SerializeMessageConnector", new SerializeMessageConnector(mainSerializer, messageMetadataRegistry), "Converts a logical message into a physical message");

        context.Services.AddSingleton(_ => mapper);
        context.Services.AddSingleton<IMessageCreator>(sp => sp.GetRequiredService<IMessageMapper>());
        context.Services.AddSingleton(_ => messageMetadataRegistry);
        context.Services.AddSingleton(_ => logicalMessageFactory);

        LogFoundMessages(messageMetadataRegistry.GetAllMessages());

        context.Settings.AddStartupDiagnosticsSection("Serialization", new
        {
            MainSerializer = new
            {
                Type = mainSerializerAndDefinition.Item1.GetType().FullName,
                Version = FileVersionRetriever.GetFileVersion(mainSerializerAndDefinition.Item1.GetType()),
                mainSerializer.ContentType
            },
            AdditionalDeserializers = additionalDeserializerDiagnostics,
            AllowMessageTypeInference = allowMessageTypeInference,
            AllowDynamicTypeLoading = allowDynamicTypeLoading
        });
    }

    static IMessageSerializer CreateMessageSerializer(Tuple<SerializationDefinition, SettingsHolder> definitionAndSettings, IMessageMapper mapper, IReadOnlySettings mainSettings)
    {
        var definition = definitionAndSettings.Item1;
        var deserializerSettings = definitionAndSettings.Item2;
        deserializerSettings.Merge(mainSettings);
        deserializerSettings.PreventChanges();

        var serializerFactory = definition.Configure(deserializerSettings);
        var serializer = serializerFactory(mapper);

        if (string.IsNullOrWhiteSpace(serializer.ContentType))
        {
            throw new ArgumentException($"Serializer '{definition.GetType().Name}' defines no content type. Ensure the '{nameof(serializer.ContentType)}' property of the serializer has a value.");
        }

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
        Logger.Debug($"Message definitions: {Environment.NewLine}{string.Join(Environment.NewLine, messageDefinitions.Select(md => md.MessageType.FullName))}");
    }

    static readonly ILog Logger = LogManager.GetLogger<SerializationFeature>();
}