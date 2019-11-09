namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Logging;
    using ObjectBuilder;
    using Transport;

    class ReceiveComponent
    {
        ReceiveComponent(ReceiveConfiguration configuration,
            TransportReceiveInfrastructure receiveInfrastructure,
            PipelineComponent pipeline,
            IEventAggregator eventAggregator,
            CriticalError criticalError,
            string errorQueue)
        {
            this.configuration = configuration;
            this.receiveInfrastructure = receiveInfrastructure;
            this.pipeline = pipeline;
            this.eventAggregator = eventAggregator;
            this.criticalError = criticalError;
            this.errorQueue = errorQueue;
        }

        public static ReceiveComponent Initialize(ReceiveConfiguration receiveConfiguration,
            TransportComponent transportComponent,
            PipelineComponent pipeline,
            QueueBindings queueBindings,
            EventAggregator eventAggregator,
            CriticalError criticalError,
            string errorQueue,
            HostingComponent hostingComponent)
        {
            TransportReceiveInfrastructure transportReceiveInfrastructure = null;

            //don't create the receive infrastructure for send-only endpoints
            if (receiveConfiguration != null)
            {
                transportReceiveInfrastructure = transportComponent.ConfigureReceiveInfrastructure();
            }

            var receiveComponent = new ReceiveComponent(receiveConfiguration,
                transportReceiveInfrastructure,
                pipeline,
                eventAggregator,
                criticalError,
                errorQueue);

            receiveComponent.BindQueues(queueBindings);

            if (receiveConfiguration != null)
            {
                hostingComponent.AddStartupDiagnosticsSection("Receiving", new
                {
                    receiveConfiguration.LocalAddress,
                    receiveConfiguration.InstanceSpecificQueue,
                    receiveConfiguration.LogicalAddress,
                    receiveConfiguration.PurgeOnStartup,
                    receiveConfiguration.QueueNameBase,
                    TransactionMode = receiveConfiguration.TransactionMode.ToString("G"),
                    receiveConfiguration.PushRuntimeSettings.MaxConcurrency,
                    Satellites = receiveConfiguration.SatelliteDefinitions.Select(s => new
                    {
                        s.Name,
                        s.ReceiveAddress,
                        TransactionMode = s.RequiredTransportTransactionMode.ToString("G"),
                        s.RuntimeSettings.MaxConcurrency
                    }).ToArray()
                });
            }

            return receiveComponent;
        }

        public async Task PrepareToStart(ContainerComponent containerComponent, RecoverabilityComponent recoverabilityComponent)
        {
            if (IsSendOnly)
            {
                return;
            }

            mainPipelineExecutor = new MainPipelineExecutor(containerComponent.Builder, pipeline);

            if (configuration.PurgeOnStartup)
            {
                Logger.Warn("All queues owned by the endpoint will be purged on startup.");
            }

            AddReceivers(containerComponent.Builder, recoverabilityComponent.GetRecoverabilityExecutorFactory(containerComponent.Builder));

            foreach (var receiver in receivers)
            {
                try
                {
                    await receiver.Init().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Logger.Fatal($"Receiver {receiver.Id} failed to initialize.", ex);
                    throw;
                }
            }
        }

        public async Task Start()
        {
            foreach (var receiver in receivers)
            {
                try
                {
                    await receiver.Start().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Logger.Fatal($"Receiver {receiver.Id} failed to start.", ex);
                    throw;
                }
            }
        }

        public Task Stop()
        {
            var receiverStopTasks = receivers.Select(async receiver =>
            {
                Logger.DebugFormat("Stopping {0} receiver", receiver.Id);
                await receiver.Stop().ConfigureAwait(false);
                Logger.DebugFormat("Stopped {0} receiver", receiver.Id);
            });

            return Task.WhenAll(receiverStopTasks);
        }

        public Task CreateQueuesIfNecessary(QueueBindings queueBindings, string username)
        {
            if (IsSendOnly)
            {
                return TaskEx.CompletedTask;
            }

            var queueCreator = receiveInfrastructure.QueueCreatorFactory();

            return queueCreator.CreateQueueIfNecessary(queueBindings, username);
        }

        public async Task PerformPreStartupChecks()
        {
            if (IsSendOnly)
            {
                return;
            }

            var result = await receiveInfrastructure.PreStartupCheck().ConfigureAwait(false);

            if (!result.Succeeded)
            {
                throw new Exception($"Pre start-up check failed: {result.ErrorMessage}");
            }
        }

        bool IsSendOnly => configuration == null;

        void BindQueues(QueueBindings queueBindings)
        {
            if (IsSendOnly)
            {
                return;
            }

            queueBindings.BindReceiving(configuration.LocalAddress);

            if (configuration.InstanceSpecificQueue != null)
            {
                queueBindings.BindReceiving(configuration.InstanceSpecificQueue);
            }

            foreach (var satellitePipeline in configuration.SatelliteDefinitions)
            {
                queueBindings.BindReceiving(satellitePipeline.ReceiveAddress);
            }
        }

        void AddReceivers(IBuilder builder, RecoverabilityExecutorFactory recoverabilityExecutorFactory)
        {
            var requiredTransactionSupport = configuration.TransactionMode;

            var recoverabilityExecutor = recoverabilityExecutorFactory.CreateDefault(eventAggregator, configuration.LocalAddress);
            var pushSettings = new PushSettings(configuration.LocalAddress, errorQueue, configuration.PurgeOnStartup, requiredTransactionSupport);
            var dequeueLimitations = configuration.PushRuntimeSettings;

            receivers.Add(new TransportReceiver(MainReceiverId, BuildMessagePump(), pushSettings, dequeueLimitations, mainPipelineExecutor, recoverabilityExecutor, criticalError));

            if (configuration.InstanceSpecificQueue != null)
            {
                var instanceSpecificQueue = configuration.InstanceSpecificQueue;
                var instanceSpecificRecoverabilityExecutor = recoverabilityExecutorFactory.CreateDefault(eventAggregator, instanceSpecificQueue);
                var sharedReceiverPushSettings = new PushSettings(instanceSpecificQueue, errorQueue, configuration.PurgeOnStartup, requiredTransactionSupport);

                receivers.Add(new TransportReceiver(MainReceiverId, BuildMessagePump(), sharedReceiverPushSettings, dequeueLimitations, mainPipelineExecutor, instanceSpecificRecoverabilityExecutor, criticalError));
            }

            foreach (var satellitePipeline in configuration.SatelliteDefinitions)
            {
                var satelliteRecoverabilityExecutor = recoverabilityExecutorFactory.Create(satellitePipeline.RecoverabilityPolicy, eventAggregator, satellitePipeline.ReceiveAddress);
                var satellitePushSettings = new PushSettings(satellitePipeline.ReceiveAddress, errorQueue, configuration.PurgeOnStartup, satellitePipeline.RequiredTransportTransactionMode);

                receivers.Add(new TransportReceiver(satellitePipeline.Name, BuildMessagePump(), satellitePushSettings, satellitePipeline.RuntimeSettings, new SatellitePipelineExecutor(builder, satellitePipeline), satelliteRecoverabilityExecutor, criticalError));
            }
        }

        IPushMessages BuildMessagePump()
        {
            return receiveInfrastructure.MessagePumpFactory();
        }

        ReceiveConfiguration configuration;
        List<TransportReceiver> receivers = new List<TransportReceiver>();
        TransportReceiveInfrastructure receiveInfrastructure;
        PipelineComponent pipeline;
        IPipelineExecutor mainPipelineExecutor;
        readonly IEventAggregator eventAggregator;
        CriticalError criticalError;
        string errorQueue;

        const string MainReceiverId = "Main";

        static ILog Logger = LogManager.GetLogger<ReceiveComponent>();
    }
}