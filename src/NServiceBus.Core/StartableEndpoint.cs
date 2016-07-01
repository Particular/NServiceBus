namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Principal;
    using System.Threading.Tasks;
    using Config;
    using ConsistencyGuarantees;
    using Features;
    using Logging;
    using ObjectBuilder;
    using Pipeline;
    using Settings;
    using Transports;

    class StartableEndpoint : IStartableEndpoint
    {
        public StartableEndpoint(SettingsHolder settings,
            IBuilder builder,
            FeatureActivator featureActivator,
            PipelineConfiguration pipelineConfiguration,
            IEventAggregator eventAggregator,
            TransportInfrastructure transportInfrastructure)
        {
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

            mainReceivePipeline = new Pipeline<ITransportReceiveContext>(builder, settings, pipelineConfiguration.Modifications);

            var receivers = CreateReceivers();

            var runningInstance = new RunningEndpointInstance(settings, builder, receivers, featureRunner, messageSession, transportInfrastructure);

            // set the started endpoint on CriticalError to pass the endpoint to the critical error action
            builder.Build<CriticalError>().SetEndpoint(runningInstance);

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
            var errorQueue = settings.ErrorQueueAddress();
            var dequeueLimitations = GeDequeueLimitationsForReceivePipeline();
            var requiredTransactionSupport = settings.GetRequiredTransactionModeForReceives();

            var receivers = BuildMainReceivers(errorQueue, purgeOnStartup, requiredTransactionSupport, dequeueLimitations).ToList();

            foreach (var satellitePipeline in settings.Get<SatelliteDefinitions>().Definitions)
            {
                var satellitePushSettings = new PushSettings(satellitePipeline.ReceiveAddress, errorQueue, purgeOnStartup, satellitePipeline.RequiredTransportTransactionMode);
                var receiver = new TransportReceiver(satellitePipeline.Name,
                    builder,
                    satellitePushSettings,
                    dequeueLimitations,
                    satellitePipeline.OnMessage,
                    (b, context) => InvokeSatelliteError(b, context));

                receivers.Add(receiver);
            }

            return receivers;
        }

        IEnumerable<TransportReceiver> BuildMainReceivers(string errorQueue, bool purgeOnStartup, TransportTransactionMode requiredTransactionSupport, PushRuntimeSettings dequeueLimitations)
        {
            var pushSettings = new PushSettings(settings.LocalAddress(), errorQueue, purgeOnStartup, requiredTransactionSupport);
            yield return new TransportReceiver(MainReceiverId,
                builder,
                pushSettings,
                dequeueLimitations,
                (b, context) => InvokePipeline(b, context),
                InvokeError);

            if (settings.InstanceSpecificQueue() != null)
            {
                var sharedReceiverPushSettings = new PushSettings(settings.InstanceSpecificQueue(), errorQueue, purgeOnStartup, requiredTransactionSupport);
                yield return new TransportReceiver(MainReceiverId,
                    builder,
                    sharedReceiverPushSettings,
                    dequeueLimitations,
                    (b, context) => InvokePipeline(b, context),
                    InvokeError);
            }
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

        async Task InvokePipeline(IBuilder rootBuilder, MessageContext messageContext)
        {
            var pipelineStartedAt = DateTime.UtcNow;

            using (var childBuilder = rootBuilder.CreateChildBuilder())
            {
                var rootContext = new RootContext(childBuilder, pipelineCache, eventAggregator);

                var message = new IncomingMessage(messageContext.MessageId, messageContext.Headers, messageContext.BodyStream);
                var context = new TransportReceiveContext(message, messageContext.TransportTransaction, messageContext.ReceiveCancellationTokenSource, rootContext);

                context.Extensions.Merge(messageContext.Context);

                await mainReceivePipeline.Invoke(context).ConfigureAwait(false);

                await context.RaiseNotification(new ReceivePipelineCompleted(message, pipelineStartedAt, DateTime.UtcNow)).ConfigureAwait(false);
            }
        }

        Task<bool> InvokeError(IBuilder rootBuilder, ErrorContext context)
        {
            return Task.FromResult(false);
        }


        Task<bool> InvokeSatelliteError(IBuilder rootBuilder, ErrorContext context)
        {
            return Task.FromResult(false);
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
        Pipeline<ITransportReceiveContext> mainReceivePipeline;
        IBuilder builder;
        FeatureActivator featureActivator;
        PipelineCache pipelineCache;
        PipelineConfiguration pipelineConfiguration;
        SettingsHolder settings;
        IEventAggregator eventAggregator;
        TransportInfrastructure transportInfrastructure;

        const string MainReceiverId = "Main";
        static ILog Logger = LogManager.GetLogger<StartableEndpoint>();
    }
}