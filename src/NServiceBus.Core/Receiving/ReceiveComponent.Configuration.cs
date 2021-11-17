namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using Transport;
    using Unicast;

    partial class ReceiveComponent
    {
        public static Configuration PrepareConfiguration(HostingComponent.Configuration hostingConfiguration, Settings settings, TransportSeam transportSeam)
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

            var transportDefinition = transportSeam.TransportDefinition;

            QueueAddress instanceSpecificQueueAddress = null;

            if (discriminator != null)
            {
                instanceSpecificQueueAddress = new QueueAddress(queueNameBase, discriminator);
            }

            var pushRuntimeSettings = settings.PushRuntimeSettings;

            var receiveConfiguration = new Configuration(
                queueNameBase,
                instanceSpecificQueueAddress,
                pushRuntimeSettings,
                purgeOnStartup,
                settings.PipelineCompletedSubscribers ?? new Notification<ReceivePipelineCompleted>(),
                isSendOnlyEndpoint,
                settings.ExecuteTheseHandlersFirst,
                settings.MessageHandlerRegistry,
                settings.ShouldCreateQueues,
                transportSeam,
                settings.Conventions);

            settings.RegisterReceiveConfigurationForBackwardsCompatibility(receiveConfiguration);

            return receiveConfiguration;
        }


        public class Configuration
        {
            public Configuration(string queueNameBase,
                QueueAddress instanceSpecificQueueAddress,
                PushRuntimeSettings pushRuntimeSettings,
                bool purgeOnStartup,
                Notification<ReceivePipelineCompleted> pipelineCompletedSubscribers,
                bool isSendOnlyEndpoint,
                List<Type> executeTheseHandlersFirst,
                MessageHandlerRegistry messageHandlerRegistry,
                bool createQueues,
                TransportSeam transportSeam,
                Conventions conventions)
            {
                QueueNameBase = queueNameBase;
                LocalQueueAddress = new QueueAddress(QueueNameBase);
                InstanceSpecificQueueAddress = instanceSpecificQueueAddress;
                PushRuntimeSettings = pushRuntimeSettings;
                PurgeOnStartup = purgeOnStartup;
                IsSendOnlyEndpoint = isSendOnlyEndpoint;
                PipelineCompletedSubscribers = pipelineCompletedSubscribers;
                ExecuteTheseHandlersFirst = executeTheseHandlersFirst;
                satelliteDefinitions = new List<SatelliteDefinition>();
                this.messageHandlerRegistry = messageHandlerRegistry;
                CreateQueues = createQueues;
                Conventions = conventions;
                this.transportSeam = transportSeam;
            }

            public QueueAddress LocalQueueAddress { get; }

            public QueueAddress InstanceSpecificQueueAddress { get; }

            public PushRuntimeSettings PushRuntimeSettings { get; }

            public string QueueNameBase { get; }

            public IReadOnlyList<SatelliteDefinition> SatelliteDefinitions => satelliteDefinitions;

            public bool PurgeOnStartup { get; }

            public bool IsSendOnlyEndpoint { get; }

            public List<Type> ExecuteTheseHandlersFirst { get; }

            public bool CreateQueues { get; }

            public Conventions Conventions { get; }

            public void AddSatelliteReceiver(string name, QueueAddress transportAddress, PushRuntimeSettings runtimeSettings, Func<RecoverabilityConfig, ErrorContext, RecoverabilityAction> recoverabilityPolicy, OnSatelliteMessage onMessage)
            {
                var satelliteDefinition = new SatelliteDefinition(name, transportAddress, runtimeSettings, recoverabilityPolicy, onMessage);

                satelliteDefinitions.Add(satelliteDefinition);
            }

            public Notification<ReceivePipelineCompleted> PipelineCompletedSubscribers;

            //This should only be used by the receive component it self
            internal readonly MessageHandlerRegistry messageHandlerRegistry;
            internal readonly TransportSeam transportSeam;

            List<SatelliteDefinition> satelliteDefinitions;
        }
    }
}