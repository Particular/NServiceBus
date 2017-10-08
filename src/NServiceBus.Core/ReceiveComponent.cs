namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using ConsistencyGuarantees;
    using Logging;
    using ObjectBuilder;
    using Routing;
    using Settings;
    using Transport;

    class ReceiveComponent
    {
        public ReceiveComponent(string endpointName, bool isSendOnlyEndpoint, TransportInfrastructure transportInfrastructure)
        {
            this.endpointName = endpointName;
            this.isSendOnlyEndpoint = isSendOnlyEndpoint;
            this.transportInfrastructure = transportInfrastructure;
        }

        public LogicalAddress LogicalAddress { get; private set; }

        public string LocalAddress { get; private set; }

        public string InstanceSpecificQueue { get; private set; }

        public string ReceiveQueueName { get; private set; }

        public void Initialize(ReadOnlySettings settings, QueueBindings queueBindings)
        {
            this.settings = settings;

            if (isSendOnlyEndpoint)
            {
                return;
            }

            var discriminator = settings.GetOrDefault<string>("EndpointInstanceDiscriminator");
            ReceiveQueueName = settings.GetOrDefault<string>("BaseInputQueueName") ?? endpointName;

            var mainInstance = transportInfrastructure.BindToLocalEndpoint(new EndpointInstance(endpointName));

            LogicalAddress = LogicalAddress.CreateLocalAddress(ReceiveQueueName, mainInstance.Properties);

            LocalAddress = transportInfrastructure.ToTransportAddress(LogicalAddress);

            queueBindings.BindReceiving(LocalAddress);

            if (discriminator != null)
            {
                InstanceSpecificQueue = transportInfrastructure.ToTransportAddress(LogicalAddress.CreateIndividualizedAddress(discriminator));

                queueBindings.BindReceiving(InstanceSpecificQueue);
            }

            receiveInfrastructure = transportInfrastructure.ConfigureReceiveInfrastructure();
        }

        public async Task InitializeReceivers(MainPipelineExecutor mainPipelineExecutor, IEventAggregator eventAggregator, IBuilder builder, CriticalError criticalError)
        {
            AddReceivers(mainPipelineExecutor, eventAggregator, builder, criticalError);

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


        void AddReceivers(MainPipelineExecutor mainPipelineExecutor, IEventAggregator eventAggregator, IBuilder builder, CriticalError criticalError)
        {
            if (isSendOnlyEndpoint)
            {
                return;
            }

            var purgeOnStartup = settings.GetOrDefault<bool>("Transport.PurgeOnStartup");
            if (purgeOnStartup)
            {
                Logger.Warn($"All queues owned by the '{endpointName}' endpoint will be purged on startup.");
            }

            var errorQueue = settings.ErrorQueueAddress();
            var requiredTransactionSupport = settings.GetRequiredTransactionModeForReceives();
            var recoverabilityExecutorFactory = builder.Build<RecoverabilityExecutorFactory>();

            var recoverabilityExecutor = recoverabilityExecutorFactory.CreateDefault(eventAggregator, LocalAddress);
            var pushSettings = new PushSettings(LocalAddress, errorQueue, purgeOnStartup, requiredTransactionSupport);
            var dequeueLimitations = GetDequeueLimitationsForReceivePipeline();

            receivers.Add(new TransportReceiver(MainReceiverId, BuildMessagePump(), pushSettings, dequeueLimitations, mainPipelineExecutor, recoverabilityExecutor, criticalError));

            if (InstanceSpecificQueue != null)
            {
                var instanceSpecificQueue = InstanceSpecificQueue;
                var instanceSpecificRecoverabilityExecutor = recoverabilityExecutorFactory.CreateDefault(eventAggregator, instanceSpecificQueue);
                var sharedReceiverPushSettings = new PushSettings(instanceSpecificQueue, errorQueue, purgeOnStartup, requiredTransactionSupport);

                receivers.Add(new TransportReceiver(MainReceiverId, BuildMessagePump(), sharedReceiverPushSettings, dequeueLimitations, mainPipelineExecutor, instanceSpecificRecoverabilityExecutor, criticalError));
            }
            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (var satellitePipeline in settings.Get<SatelliteDefinitions>().Definitions)
            {
                var satelliteRecoverabilityExecutor = recoverabilityExecutorFactory.Create(satellitePipeline.RecoverabilityPolicy, eventAggregator, satellitePipeline.ReceiveAddress);
                var satellitePushSettings = new PushSettings(satellitePipeline.ReceiveAddress, errorQueue, purgeOnStartup, satellitePipeline.RequiredTransportTransactionMode);

                receivers.Add(new TransportReceiver(satellitePipeline.Name, BuildMessagePump(), satellitePushSettings, satellitePipeline.RuntimeSettings, new SatellitePipelineExecutor(builder, satellitePipeline), satelliteRecoverabilityExecutor, criticalError));
            }
        }

        public Task CreateQueuesIfNecessary(string username)
        {
            if (isSendOnlyEndpoint)
            {
                return TaskEx.CompletedTask;
            }

            var queueCreator = receiveInfrastructure.QueueCreatorFactory();
            var queueBindings = settings.Get<QueueBindings>();

            return queueCreator.CreateQueueIfNecessary(queueBindings, username);
        }

        public async Task PerformPreStartupChecks()
        {
            if (isSendOnlyEndpoint)
            {
                return;
            }

            var result = await receiveInfrastructure.PreStartupCheck().ConfigureAwait(false);

            if (!result.Succeeded)
            {
                throw new Exception($"Pre start-up check failed: {result.ErrorMessage}");
            }
        }

        //note: this should be handled in a feature but we don't have a good
        // extension point to plugin atm
        PushRuntimeSettings GetDequeueLimitationsForReceivePipeline()
        {
            if (settings.TryGet(out MessageProcessingOptimizationExtensions.ConcurrencyLimit concurrencyLimit))
            {
                return new PushRuntimeSettings(concurrencyLimit.MaxValue);
            }

            return PushRuntimeSettings.Default;
        }

        IPushMessages BuildMessagePump()
        {
            return receiveInfrastructure.MessagePumpFactory();
        }


        List<TransportReceiver> receivers = new List<TransportReceiver>();
        TransportReceiveInfrastructure receiveInfrastructure;
        TransportInfrastructure transportInfrastructure;

        string endpointName;
        bool isSendOnlyEndpoint;
        ReadOnlySettings settings;

        const string MainReceiverId = "Main";

        static ILog Logger = LogManager.GetLogger<ReceiveComponent>();
    }
}