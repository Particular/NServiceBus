namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Security.Principal;
    using System.Threading.Tasks;
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
            await transportInfrastructure.Start().ConfigureAwait(false);

            AppDomain.CurrentDomain.SetPrincipalPolicy(PrincipalPolicy.WindowsPrincipal);

            var mainPipeline = new Pipeline<ITransportReceiveContext>(builder, settings, pipelineConfiguration.Modifications);
            var receivers = CreateReceivers(mainPipeline);
            await InitializeReceivers(receivers).ConfigureAwait(false);

            var featureRunner = await StartFeatures(messageSession).ConfigureAwait(false);

            var runningInstance = new RunningEndpointInstance(settings, builder, receivers, featureRunner, messageSession, transportInfrastructure);
            // set the started endpoint on CriticalError to pass the endpoint to the critical error action
            criticalError.SetEndpoint(runningInstance);

            StartReceivers(receivers);

            return runningInstance;
        }

        async Task<FeatureRunner> StartFeatures(IMessageSession session)
        {
            var featureRunner = new FeatureRunner(featureActivator);
            await featureRunner.Start(builder, session).ConfigureAwait(false);
            return featureRunner;
        }

        static async Task InitializeReceivers(List<TransportReceiver> receivers)
        {
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

        static void StartReceivers(List<TransportReceiver> receivers)
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

        List<TransportReceiver> CreateReceivers(IPipeline<ITransportReceiveContext> mainPipeline)
        {
            if (settings.GetOrDefault<bool>("Endpoint.SendOnly"))
            {
                return new List<TransportReceiver>();
            }

            var purgeOnStartup = settings.GetOrDefault<bool>("Transport.PurgeOnStartup");
            if (purgeOnStartup)
            {
                Logger.Warn($"All queues owned by the '{settings.EndpointName()}' endpoint will be purged on startup.");
            }

            var errorQueue = settings.ErrorQueueAddress();
            var requiredTransactionSupport = settings.GetRequiredTransactionModeForReceives();
            var recoverabilityExecutorFactory = builder.Build<RecoverabilityExecutorFactory>();

            var receivers = BuildMainReceivers(errorQueue, purgeOnStartup, requiredTransactionSupport, recoverabilityExecutorFactory, mainPipeline);

            foreach (var satellitePipeline in settings.Get<SatelliteDefinitions>().Definitions)
            {
                var satelliteRecoverabilityExecutor = recoverabilityExecutorFactory.Create(satellitePipeline.RecoverabilityPolicy, eventAggregator, satellitePipeline.ReceiveAddress);
                var satellitePushSettings = new PushSettings(satellitePipeline.ReceiveAddress, errorQueue, purgeOnStartup, satellitePipeline.RequiredTransportTransactionMode);

                receivers.Add(new TransportReceiver(satellitePipeline.Name, pipelineCache, builder.Build<IPushMessages>(), satellitePushSettings, satellitePipeline.RuntimeSettings, new SatellitePipelineExecutor(builder, satellitePipeline, pipelineCache), satelliteRecoverabilityExecutor, criticalError));
            }

            return receivers;
        }

        List<TransportReceiver> BuildMainReceivers(string errorQueue, bool purgeOnStartup, TransportTransactionMode requiredTransactionSupport, RecoverabilityExecutorFactory recoverabilityExecutorFactory, IPipeline<ITransportReceiveContext> mainPipeline)
        {
            var localAddress = settings.LocalAddress();

            var recoverabilityExecutor = recoverabilityExecutorFactory.CreateDefault(eventAggregator, localAddress);
            var pushSettings = new PushSettings(settings.LocalAddress(), errorQueue, purgeOnStartup, requiredTransactionSupport);
            var mainPipelineExecutor = new MainPipelineExecutor(builder, eventAggregator, pipelineCache, mainPipeline);
            var dequeueLimitations = GetDequeueLimitationsForReceivePipeline();

            var receivers = new List<TransportReceiver>();

            receivers.Add(new TransportReceiver(MainReceiverId, pipelineCache, builder.Build<IPushMessages>(), pushSettings, dequeueLimitations, mainPipelineExecutor, recoverabilityExecutor, criticalError));

            if (settings.InstanceSpecificQueue() != null)
            {
                var instanceSpecificQueue = settings.InstanceSpecificQueue();
                var instanceSpecificRecoverabilityExecutor = recoverabilityExecutorFactory.CreateDefault(eventAggregator, instanceSpecificQueue);
                var sharedReceiverPushSettings = new PushSettings(settings.InstanceSpecificQueue(), errorQueue, purgeOnStartup, requiredTransactionSupport);

                receivers.Add(new TransportReceiver(MainReceiverId, pipelineCache, builder.Build<IPushMessages>(), sharedReceiverPushSettings, dequeueLimitations, mainPipelineExecutor, instanceSpecificRecoverabilityExecutor, criticalError));
            }

            return receivers;
        }

        //note: this should be handled in a feature but we don't have a good
        // extension point to plugin atm
        PushRuntimeSettings GetDequeueLimitationsForReceivePipeline()
        {
            MessageProcessingOptimizationExtensions.ConcurrencyLimit concurrencyLimit;

            if (settings.TryGet(out concurrencyLimit))
            {
                return new PushRuntimeSettings(concurrencyLimit.MaxValue);
            }

            return PushRuntimeSettings.Default;
        }

        IMessageSession messageSession;
        IBuilder builder;
        FeatureActivator featureActivator;

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