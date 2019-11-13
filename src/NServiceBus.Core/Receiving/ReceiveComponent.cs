namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Logging;
    using MessageInterfaces.MessageMapper.Reflection;
    using ObjectBuilder;
    using Transport;

    class ReceiveComponent
    {
        ReceiveComponent(ReceiveConfiguration configuration,
            Func<IPushMessages> messagePumpFactory,
            PipelineComponent pipeline,
            IEventAggregator eventAggregator,
            CriticalError criticalError,
            string errorQueue)
        {
            this.configuration = configuration;
            this.messagePumpFactory = messagePumpFactory;
            this.pipeline = pipeline;
            this.eventAggregator = eventAggregator;
            this.criticalError = criticalError;
            this.errorQueue = errorQueue;
        }

        public static ReceiveComponent Initialize(ReceiveConfiguration receiveConfiguration,
            TransportComponent transportComponent,
            PipelineComponent pipeline,
            EventAggregator eventAggregator,
            string errorQueue,
            HostingComponent hostingComponent)
        {
            Func<IPushMessages> messagePumpFactory = null;

            //we don't need the message pump factory for send-only endpoints
            if (receiveConfiguration != null)
            {
                messagePumpFactory = transportComponent.GetMessagePumpFactory();
            }

            var receiveComponent = new ReceiveComponent(receiveConfiguration,
                messagePumpFactory,
                pipeline,
                eventAggregator,
                hostingComponent.CriticalError,
                errorQueue);

            receiveComponent.BindQueues(transportComponent.QueueBindings);

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

        public async Task PrepareToStart(IBuilder builder, RecoverabilityComponent recoverabilityComponent, SendComponent sendComponent)
        {
            if (IsSendOnly)
            {
                return;
            }

            mainPipelineExecutor = new MainPipelineExecutor(builder, pipeline, sendComponent);

            if (configuration.PurgeOnStartup)
            {
                Logger.Warn("All queues owned by the endpoint will be purged on startup.");
            }

            AddReceivers(builder, recoverabilityComponent.GetRecoverabilityExecutorFactory(builder));

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
            return messagePumpFactory();
        }

        ReceiveConfiguration configuration;
        List<TransportReceiver> receivers = new List<TransportReceiver>();
        Func<IPushMessages> messagePumpFactory;
        PipelineComponent pipeline;
        IPipelineExecutor mainPipelineExecutor;
        readonly IEventAggregator eventAggregator;
        CriticalError criticalError;
        string errorQueue;

        const string MainReceiverId = "Main";

        static ILog Logger = LogManager.GetLogger<ReceiveComponent>();
    }
}