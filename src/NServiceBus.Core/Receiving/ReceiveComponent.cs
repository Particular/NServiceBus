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
        public ReceiveComponent(ReceiveConfiguration configuration, TransportReceiveInfrastructure receiveInfrastructure, QueueBindings queueBindings)
        {
            this.configuration = configuration;
            this.receiveInfrastructure = receiveInfrastructure;
            this.queueBindings = queueBindings;
        }

        public async Task Initialize(IPipelineExecutor mainPipelineExecutor, IEventAggregator eventAggregator, IBuilder builder, CriticalError criticalError, string errorQueue)
        {
            if (!configuration.IsEnabled)
            {
                return;
            }

            if (configuration.PurgeOnStartup)
            {
                Logger.Warn("All queues owned by the endpoint will be purged on startup.");
            }

            AddReceivers(mainPipelineExecutor, eventAggregator, builder, criticalError, errorQueue);

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

        public void Start()
        {
            foreach (var receiver in receivers)
            {
                try
                {
                    receiver.Start();
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

        public Task CreateQueuesIfNecessary(string username)
        {
            if (!configuration.IsEnabled)
            {
                return TaskEx.CompletedTask;
            }

            var queueCreator = receiveInfrastructure.QueueCreatorFactory();

            return queueCreator.CreateQueueIfNecessary(queueBindings, username);
        }

        public async Task PerformPreStartupChecks()
        {
            if (!configuration.IsEnabled)
            {
                return;
            }

            var result = await receiveInfrastructure.PreStartupCheck().ConfigureAwait(false);

            if (!result.Succeeded)
            {
                throw new Exception($"Pre start-up check failed: {result.ErrorMessage}");
            }
        }

        void AddReceivers(IPipelineExecutor mainPipelineExecutor, IEventAggregator eventAggregator, IBuilder builder, CriticalError criticalError, string errorQueue)
        {
            var requiredTransactionSupport = configuration.TransactionMode;
            var recoverabilityExecutorFactory = builder.Build<RecoverabilityExecutorFactory>();

            var recoverabilityExecutor = recoverabilityExecutorFactory.CreateDefault(eventAggregator, configuration.LocalAddress);
            var pushSettings = new PushSettings(configuration.LocalAddress, errorQueue, configuration.PurgeOnStartup, requiredTransactionSupport);
            var dequeueLimitations = configuration.PushRuntimeSettings;

            receivers.Add(new TransportReceiver(MainReceiverId, BuildMessagePump(), pushSettings, dequeueLimitations, mainPipelineExecutor, recoverabilityExecutor, criticalError));
            queueBindings.BindReceiving(configuration.LocalAddress);

            if (configuration.InstanceSpecificQueue != null)
            {
                var instanceSpecificQueue = configuration.InstanceSpecificQueue;
                var instanceSpecificRecoverabilityExecutor = recoverabilityExecutorFactory.CreateDefault(eventAggregator, instanceSpecificQueue);
                var sharedReceiverPushSettings = new PushSettings(instanceSpecificQueue, errorQueue, configuration.PurgeOnStartup, requiredTransactionSupport);

                receivers.Add(new TransportReceiver(MainReceiverId, BuildMessagePump(), sharedReceiverPushSettings, dequeueLimitations, mainPipelineExecutor, instanceSpecificRecoverabilityExecutor, criticalError));
                queueBindings.BindReceiving(instanceSpecificQueue);
            }
            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (var satellitePipeline in configuration.SatelliteDefinitions.Definitions)
            {
                var satelliteRecoverabilityExecutor = recoverabilityExecutorFactory.Create(satellitePipeline.RecoverabilityPolicy, eventAggregator, satellitePipeline.ReceiveAddress);
                var satellitePushSettings = new PushSettings(satellitePipeline.ReceiveAddress, errorQueue, configuration.PurgeOnStartup, satellitePipeline.RequiredTransportTransactionMode);

                receivers.Add(new TransportReceiver(satellitePipeline.Name, BuildMessagePump(), satellitePushSettings, satellitePipeline.RuntimeSettings, new SatellitePipelineExecutor(builder, satellitePipeline), satelliteRecoverabilityExecutor, criticalError));
                queueBindings.BindReceiving(satellitePipeline.ReceiveAddress);
            }
        }

        IPushMessages BuildMessagePump()
        {
            return receiveInfrastructure.MessagePumpFactory();
        }

        ReceiveConfiguration configuration;
        List<TransportReceiver> receivers = new List<TransportReceiver>();
        TransportReceiveInfrastructure receiveInfrastructure;
        QueueBindings queueBindings;

        const string MainReceiverId = "Main";

        static ILog Logger = LogManager.GetLogger<ReceiveComponent>();
    }
}