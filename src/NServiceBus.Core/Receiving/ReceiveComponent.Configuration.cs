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

            if (isSendOnlyEndpoint && settings.CustomLocalAddressProvided)
            {
                throw new Exception($"Specifying a base name for the input queue using `{nameof(ReceiveSettingsExtensions.OverrideLocalAddress)}(baseInputQueueName)` is not supported for send-only endpoints.");
            }

            var endpointName = settings.EndpointName;
            var discriminator = settings.EndpointInstanceDiscriminator;
            var queueNameBase = settings.CustomLocalAddress ?? endpointName;
            var purgeOnStartup = settings.PurgeOnStartup;

            var transportDefinition = transportSeam.TransportDefinition;
            var localAddress = transportDefinition.ToTransportAddress(new QueueAddress(queueNameBase, null, null, null));

            string instanceSpecificQueue = null;
            if (discriminator != null)
            {
                instanceSpecificQueue = transportDefinition.ToTransportAddress(new QueueAddress(queueNameBase, discriminator, null, null));
            }

            var pushRuntimeSettings = settings.PushRuntimeSettings;

            var receiveConfiguration = new Configuration(
                queueNameBase,
                localAddress,
                settings.EndpointInstanceDiscriminator,
                instanceSpecificQueue,
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
                string localAddress,
                string instanceDiscriminator,
                string instanceSpecificQueue,
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
                LocalQueueAddress = new QueueAddress(QueueNameBase, null, null, null);
                LocalAddress = localAddress;
                InstanceDiscriminator = instanceDiscriminator;

                if (instanceDiscriminator != null)
                {
                    InstanceSpecificQueueAddress = new QueueAddress(QueueNameBase, instanceDiscriminator, null, null);
                }

                InstanceSpecificQueue = instanceSpecificQueue;
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

            public string LocalAddress { get; }

            public QueueAddress LocalQueueAddress { get; }

            public string InstanceDiscriminator { get; }

            public QueueAddress InstanceSpecificQueueAddress { get; }

            public string InstanceSpecificQueue { get; }

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