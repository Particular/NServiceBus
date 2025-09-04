namespace NServiceBus;

using System;
using System.Collections.Generic;
using System.Linq;
using NServiceBus.Receiving;
using Transport;
using Unicast;
using static NServiceBus.ReceiveManifest;

partial class ReceiveComponent
{
    public ReceiveManifest GetManifest(Conventions conventions)
    {
        var messageTypes = configuration.MessageHandlerRegistry.GetMessageTypes();

        return new ReceiveManifest
        {
            HandledMessages = messageTypes.Select(
                    type => new HandledMessage
                    {
                        MessageType = type,
                        IsMessage = conventions.IsMessageType(type),
                        IsCommand = conventions.IsCommandType(type),
                        IsEvent = conventions.IsEventType(type),
                    }).ToArray(),
            EventTypes = messageTypes
                .GetHandledEventTypes(conventions)
                .Select(type => type.FullName)
                .ToArray()
        };
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