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
        }

        public async Task<IEndpointInstance> Start()
        {
            DetectThrottlingConfig();

            var pipelineCache = new PipelineCache(builder, settings);
            await transportInfrastructure.Start().ConfigureAwait(false);
            var messageSession = CreateMessageSession(pipelineCache);

            var featureRunner = await StartFeatures(messageSession).ConfigureAwait(false);

            AppDomain.CurrentDomain.SetPrincipalPolicy(PrincipalPolicy.WindowsPrincipal);

            var pipelineCollection = CreateIncomingPipelines(pipelineCache);

            var runningInstance = new RunningEndpointInstance(settings, builder, pipelineCollection, featureRunner, messageSession, transportInfrastructure);

            // set the started endpoint on CriticalError to pass the endpoint to the critical error action
            builder.Build<CriticalError>().SetEndpoint(runningInstance);

            await StartPipelines(pipelineCollection).ConfigureAwait(false);

            return runningInstance;
        }

        static Task StartPipelines(PipelineCollection pipelineCollection)
        {
            return pipelineCollection.Start();
        }

        PipelineCollection CreateIncomingPipelines(IPipelineCache pipelineCache)
        {
            PipelineCollection pipelineCollection;
            if (settings.GetOrDefault<bool>("Endpoint.SendOnly"))
            {
                pipelineCollection = new PipelineCollection(Enumerable.Empty<TransportReceiver>());
            }
            else
            {
                var pipelines = BuildPipelines(pipelineCache).ToArray();
                pipelineCollection = new PipelineCollection(pipelines);
            }

            return pipelineCollection;
        }

        async Task<FeatureRunner> StartFeatures(IMessageSession session)
        {
            var featureRunner = new FeatureRunner(featureActivator);
            await featureRunner.Start(builder, session).ConfigureAwait(false);
            return featureRunner;
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

        IMessageSession CreateMessageSession(IPipelineCache cache)
        {
            var rootContext = new RootContext(builder, cache, eventAggregator);
            return new MessageSession(rootContext);
        }

        IEnumerable<TransportReceiver> BuildPipelines(IPipelineCache cache)
        {
            var purgeOnStartup = settings.GetOrDefault<bool>("Transport.PurgeOnStartup");
            var errorQueue = settings.ErrorQueueAddress();
            var dequeueLimitations = GeDequeueLimitationsForReceivePipeline();
            var requiredTransactionSupport = settings.GetRequiredTransactionModeForReceives();

            var mainPipelineInstance = new Pipeline<ITransportReceiveContext>(builder, settings, pipelineConfiguration.Modifications);

            var pushSettings = new PushSettings(settings.LocalAddress(), errorQueue, purgeOnStartup, requiredTransactionSupport);
            yield return new TransportReceiver(MainPipelineId,builder, pushSettings, dequeueLimitations, (b,context) => InvokePipeline(b,cache, mainPipelineInstance, context));

            if (settings.InstanceSpecificQueue() != null)
            {
                var sharedReceiverPushSettings = new PushSettings(settings.InstanceSpecificQueue(), errorQueue, purgeOnStartup, requiredTransactionSupport);
                yield return new TransportReceiver(MainPipelineId, builder, sharedReceiverPushSettings, dequeueLimitations, (b, context) => InvokePipeline(b,cache, mainPipelineInstance, context));
            }

            foreach (var satellitePipeline in settings.Get<SatelliteDefinitions>().Definitions)
            {
                var satellitePushSettings = new PushSettings(satellitePipeline.ReceiveAddress, errorQueue, purgeOnStartup, satellitePipeline.RequiredTransportTransactionMode);
                yield return new TransportReceiver(satellitePipeline.Name, builder, satellitePushSettings, dequeueLimitations, satellitePipeline.OnMessage);
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

        async Task InvokePipeline(IBuilder rootBuilder,IPipelineCache cache,IPipeline<ITransportReceiveContext> pipeline,PushContext pushContext)
        {
            var pipelineStartedAt = DateTime.UtcNow;

            using (var childBuilder = rootBuilder.CreateChildBuilder())
            {
                var rootContext = new RootContext(childBuilder, cache, eventAggregator);

                var message = new IncomingMessage(pushContext.MessageId, pushContext.Headers, pushContext.BodyStream);
                var context = new TransportReceiveContext(message, pushContext.TransportTransaction, pushContext.ReceiveCancellationTokenSource, rootContext);

                context.Extensions.Merge(pushContext.Context);

                await pipeline.Invoke(context).ConfigureAwait(false);

                    await context.RaiseNotification(new ReceivePipelineCompleted(message, pipelineStartedAt, DateTime.UtcNow)).ConfigureAwait(false);
            }
        }

        IBuilder builder;
        FeatureActivator featureActivator;
        PipelineConfiguration pipelineConfiguration;
        SettingsHolder settings;
        IEventAggregator eventAggregator;
        TransportInfrastructure transportInfrastructure;

        const string MainPipelineId = "Main";
    }
}