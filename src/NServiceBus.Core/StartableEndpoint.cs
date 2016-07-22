namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Security.Principal;
    using System.Threading.Tasks;
    using Config;
    using ConsistencyGuarantees;
    using Features;
    using Logging;
    using ObjectBuilder;
    using Pipeline;
    using Settings;
    using Transport;

    class StartableEndpoint : IStartableEndpoint
    {
        public StartableEndpoint(SettingsHolder settings, IBuilder builder, FeatureActivator featureActivator, PipelineConfiguration pipelineConfiguration, IEventAggregator eventAggregator, TransportInfrastructure transportInfrastructure, CriticalError criticalError)
        {
            this.criticalError = criticalError;
            this.settings = settings;
            this.builder = builder;
            this.featureActivator = featureActivator;
            this.pipelineConfiguration = pipelineConfiguration;
            this.eventAggregator = eventAggregator;
            this.transportInfrastructure = transportInfrastructure;

            pipelineCache = new PipelineCache(builder, settings);

            messageSession = new MessageSession(new RootContext(builder, pipelineCache, eventAggregator));
        }

        public async Task<IEndpointInstance> Start()
        {
            DetectThrottlingConfig();

            await transportInfrastructure.Start().ConfigureAwait(false);

            var featureRunner = await StartFeatures(messageSession).ConfigureAwait(false);

            AppDomain.CurrentDomain.SetPrincipalPolicy(PrincipalPolicy.WindowsPrincipal);

            mainPipeline = new Pipeline<ITransportReceiveContext>(builder, settings, pipelineConfiguration.Modifications);

            var receivers = CreateReceivers();

            var runningInstance = new RunningEndpointInstance(settings, builder, receivers, featureRunner, messageSession, transportInfrastructure);

            // set the started endpoint on CriticalError to pass the endpoint to the critical error action
            criticalError.SetEndpoint(runningInstance);

            await StartReceivers(receivers).ConfigureAwait(false);

            return runningInstance;
        }

        async Task<FeatureRunner> StartFeatures(IMessageSession session)
        {
            var featureRunner = new FeatureRunner(featureActivator);
            await featureRunner.Start(builder, session).ConfigureAwait(false);
            return featureRunner;
        }

        static async Task StartReceivers(List<TransportReceiver> receivers)
        {
            foreach (var receiver in receivers)
            {
                Logger.DebugFormat("Starting {0} receiver", receiver.Id);
                try
                {
                    await receiver.Start().ConfigureAwait(false);
                    Logger.DebugFormat("Started {0} receiver", receiver.Id);
                }
                catch (Exception ex)
                {
                    Logger.Fatal($"Receiver {receiver.Id} failed to start.", ex);
                    throw;
                }
            }
        }

        List<TransportReceiver> CreateReceivers()
        {
            if (settings.GetOrDefault<bool>("Endpoint.SendOnly"))
            {
                return new List<TransportReceiver>();
            }

            var purgeOnStartup = settings.GetOrDefault<bool>("Transport.PurgeOnStartup");
            var dequeueLimitations = GeDequeueLimitationsForReceivePipeline();
            var requiredTransactionSupport = settings.GetRequiredTransactionModeForReceives();
            var recoverabilityExecutorFactory = builder.Build<RecoverabilityExecutorFactory>();

            var receivers = BuildMainReceivers(purgeOnStartup, requiredTransactionSupport, dequeueLimitations, recoverabilityExecutorFactory);

            foreach (var satellitePipeline in settings.Get<SatelliteDefinitions>().Definitions)
            {
                var satelliteRecoverabilityExecutor = recoverabilityExecutorFactory.Create(satellitePipeline.RecoverabilityPolicy, eventAggregator, satellitePipeline.ReceiveAddress);
                var satellitePushSettings = new PushSettings(satellitePipeline.ReceiveAddress, purgeOnStartup, satellitePipeline.RequiredTransportTransactionMode);

                receivers.Add(new TransportReceiver(satellitePipeline.Name, builder.Build<IPushMessages>(), satellitePushSettings, dequeueLimitations, new SatellitePipelineExecutor(builder, satellitePipeline), satelliteRecoverabilityExecutor, criticalError));
            }

            return receivers;
        }

        List<TransportReceiver> BuildMainReceivers(bool purgeOnStartup, TransportTransactionMode requiredTransactionSupport, PushRuntimeSettings dequeueLimitations, RecoverabilityExecutorFactory recoverabilityExecutorFactory)
        {
            var localAddress = settings.LocalAddress();
            var recoverabilityExecutor = recoverabilityExecutorFactory.CreateDefault(eventAggregator, localAddress);
            var pushSettings = new PushSettings(settings.LocalAddress(), purgeOnStartup, requiredTransactionSupport);
            var mainPipelineExecutor = new MainPipelineExecutor(builder, eventAggregator, pipelineCache, mainPipeline);

            var receivers = new List<TransportReceiver>();

            receivers.Add(new TransportReceiver(MainReceiverId, builder.Build<IPushMessages>(), pushSettings, dequeueLimitations, mainPipelineExecutor, recoverabilityExecutor, criticalError));

            if (settings.InstanceSpecificQueue() != null)
            {
                var instanceSpecificQueue = settings.InstanceSpecificQueue();
                var instanceSpecificRecoverabilityExecutor = recoverabilityExecutorFactory.CreateDefault(eventAggregator, instanceSpecificQueue);
                var sharedReceiverPushSettings = new PushSettings(settings.InstanceSpecificQueue(), purgeOnStartup, requiredTransactionSupport);

                receivers.Add(new TransportReceiver(MainReceiverId, builder.Build<IPushMessages>(), sharedReceiverPushSettings, dequeueLimitations, mainPipelineExecutor, instanceSpecificRecoverabilityExecutor, criticalError));
            }

            return receivers;
        }

        //note: this should be handled in a feature but we don't have a good
        // extension point to plugin atm
        PushRuntimeSettings GeDequeueLimitationsForReceivePipeline()
        {
            var transportConfig = settings.GetConfigSection<TransportConfig>();

            if (transportConfig != null && transportConfig.MaximumConcurrencyLevel != 0)
            {
                throw new NotSupportedException($"The TransportConfig.MaximumConcurrencyLevel has been removed. Remove the '{nameof(TransportConfig.MaximumMessageThroughputPerSecond)}' attribute from the '{nameof(TransportConfig)}' configuration section and use 'EndpointConfiguration.LimitMessageProcessingConcurrencyTo' instead.");
            }

            MessageProcessingOptimizationExtensions.ConcurrencyLimit concurrencyLimit;

            if (settings.TryGet(out concurrencyLimit))
            {
                return new PushRuntimeSettings(concurrencyLimit.MaxValue);
            }

            return PushRuntimeSettings.Default;
        }

        [ObsoleteEx(Message = "Not needed anymore", RemoveInVersion = "7.0")]
        void DetectThrottlingConfig()
        {
            var throughputConfiguration = settings.GetConfigSection<TransportConfig>()?.MaximumMessageThroughputPerSecond;
            if (throughputConfiguration.HasValue && throughputConfiguration != -1)
            {
                throw new NotSupportedException($"Message throughput throttling has been removed. Remove the '{nameof(TransportConfig.MaximumMessageThroughputPerSecond)}' attribute from the '{nameof(TransportConfig)}' configuration section and consult the documentation for further information.");
            }
        }

        IMessageSession messageSession;
        IBuilder builder;
        FeatureActivator featureActivator;

        IPipeline<ITransportReceiveContext> mainPipeline;
        IPipelineCache pipelineCache;
        PipelineConfiguration pipelineConfiguration;

        SettingsHolder settings;
        IEventAggregator eventAggregator;
        TransportInfrastructure transportInfrastructure;
        CriticalError criticalError;

        const string MainReceiverId = "Main";
        static ILog Logger = LogManager.GetLogger<StartableEndpoint>();
    }
}