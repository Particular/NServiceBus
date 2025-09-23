namespace NServiceBus;

using System;
using System.Collections.Generic;
using System.Linq;
using NServiceBus.Settings;
using Transport;
using Unicast;

partial class ReceiveComponent
{
    public void AddManifest(HostingComponent.Configuration hostingConfiguration, SettingsHolder settings)
    {
        var messageTypes = configuration.MessageHandlerRegistry.GetMessageTypes();
        var conventions = settings.Get<Conventions>();

        var handledMessages = messageTypes.Select(
                    type => new
                    {
                        MessageType = type,
                        IsMessage = conventions.IsMessageType(type),
                        IsCommand = conventions.IsCommandType(type),
                        IsEvent = conventions.IsEventType(type),
                    });

        hostingConfiguration.AddStartupDiagnosticsSection("Manifest-MessageTypes",
            handledMessages.Select(
                handledMessage => new ReceiveComponentManifestMessageType
                {
                    Name = handledMessage.MessageType.Name,
                    FullName = handledMessage.MessageType.FullName,
                    IsMessage = handledMessage.IsMessage,
                    IsEvent = handledMessage.IsEvent,
                    IsCommand = handledMessage.IsCommand,
                    Schema = handledMessage.MessageType.GetProperties().Select(
                        prop => new ReceiveComponentManifestMessageType.SchemaProperty
                        {
                            Name = prop.Name,
                            Type = prop.PropertyType.Name,
                        }).ToArray()
                }).ToArray());
    }

    record ReceiveComponentManifestMessageType
    {
        public record SchemaProperty
        {
            public string Name { get; init; }
            public string Type { get; init; }
        }

        public string Name { get; init; }
        public string FullName { get; init; }
        public bool IsMessage { get; init; }
        public bool IsEvent { get; init; }
        public bool IsCommand { get; init; }
        public SchemaProperty[] Schema { get; init; }
    }

    public static Configuration PrepareConfiguration(Settings settings, TransportSeam transportSeam)
    {
        var isSendOnlyEndpoint = settings.IsSendOnlyEndpoint;

        if (isSendOnlyEndpoint && settings.CustomQueueNameBaseProvided)
        {
            throw new Exception($"Specifying a base name for the input queue using `{nameof(ReceiveSettingsExtensions.OverrideLocalAddress)}(baseInputQueueName)` is not supported for send-only endpoints.");
        }

        var endpointName = settings.EndpointName;
        var discriminator = settings.EndpointInstanceDiscriminator;
        var queueNameBase = settings.CustomQueueNameBase ?? endpointName;
        var purgeOnStartup = settings.PurgeOnStartup;

        QueueAddress instanceSpecificQueueAddress = null;

        if (discriminator != null)
        {
            instanceSpecificQueueAddress = new QueueAddress(queueNameBase, discriminator);
        }

        var pushRuntimeSettings = settings.PushRuntimeSettings;

        var receiveConfiguration = new Configuration(
            new QueueAddress(queueNameBase),
            instanceSpecificQueueAddress,
            pushRuntimeSettings,
            purgeOnStartup,
            settings.PipelineCompletedSubscribers ?? new Notification<ReceivePipelineCompleted>(),
            isSendOnlyEndpoint,
            settings.ExecuteTheseHandlersFirst,
            settings.MessageHandlerRegistry,
            transportSeam);

        settings.RegisterReceiveConfigurationForBackwardsCompatibility(receiveConfiguration);

        return receiveConfiguration;
    }

    public class Configuration(QueueAddress localQueueAddress,
        QueueAddress instanceSpecificQueueAddress,
        PushRuntimeSettings pushRuntimeSettings,
        bool purgeOnStartup,
        Notification<ReceivePipelineCompleted> pipelineCompletedSubscribers,
        bool isSendOnlyEndpoint,
        List<Type> executeTheseHandlersFirst,
        MessageHandlerRegistry messageHandlerRegistry,
        TransportSeam transportSeam)
    {
        public QueueAddress LocalQueueAddress { get; } = localQueueAddress;

        public QueueAddress InstanceSpecificQueueAddress { get; } = instanceSpecificQueueAddress;

        public PushRuntimeSettings PushRuntimeSettings { get; } = pushRuntimeSettings;

        public IReadOnlyList<SatelliteDefinition> SatelliteDefinitions => satelliteDefinitions;

        public bool PurgeOnStartup { get; } = purgeOnStartup;

        public bool IsSendOnlyEndpoint { get; } = isSendOnlyEndpoint;

        public List<Type> ExecuteTheseHandlersFirst { get; } = executeTheseHandlersFirst;

        public MessageHandlerRegistry MessageHandlerRegistry { get; } = messageHandlerRegistry;

        public TransportSeam TransportSeam { get; } = transportSeam;

        public Notification<ReceivePipelineCompleted> PipelineCompletedSubscribers { get; } = pipelineCompletedSubscribers;

        public void AddSatelliteReceiver(string name, QueueAddress transportAddress, PushRuntimeSettings runtimeSettings, Func<RecoverabilityConfig, ErrorContext, RecoverabilityAction> recoverabilityPolicy, OnSatelliteMessage onMessage)
        {
            var satelliteDefinition = new SatelliteDefinition(name, transportAddress, runtimeSettings, recoverabilityPolicy, onMessage);

            satelliteDefinitions.Add(satelliteDefinition);
        }

        readonly List<SatelliteDefinition> satelliteDefinitions = [];
    }
}