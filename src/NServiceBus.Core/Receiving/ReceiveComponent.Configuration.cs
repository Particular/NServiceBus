namespace NServiceBus;

using System;
using System.Collections.Generic;
using Transport;
using Unicast;

partial class ReceiveComponent
{
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

        public void AddSatelliteReceiver(string name, QueueAddress transportAddress, PushRuntimeSettings runtimeSettings, Func<RecoverabilityConfig, ErrorContext, RecoverabilityAction> recoverabilityPolicy, OnSatelliteMessage onMessage)
        {
            var satelliteDefinition = new SatelliteDefinition(name, transportAddress, runtimeSettings, recoverabilityPolicy, onMessage);

            satelliteDefinitions.Add(satelliteDefinition);
        }

        public readonly Notification<ReceivePipelineCompleted> PipelineCompletedSubscribers = pipelineCompletedSubscribers;

        //This should only be used by the receive component itself
        internal readonly MessageHandlerRegistry MessageHandlerRegistry = messageHandlerRegistry;
        internal readonly TransportSeam TransportSeam = transportSeam;

        readonly List<SatelliteDefinition> satelliteDefinitions = [];
    }
}