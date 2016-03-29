namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Security.Principal;
    using System.Threading.Tasks;
    using Config;
    using ConsistencyGuarantees;
    using Features;
    using Installation;
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

            await RunInstallers().ConfigureAwait(false);
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

        async Task RunInstallers()
        {
            if (Debugger.IsAttached || settings.GetOrDefault<bool>("Installers.Enable"))
            {
                var username = GetInstallationUserName();
                foreach (var installer in builder.BuildAll<INeedToInstallSomething>())
                {
                    await installer.Install(username).ConfigureAwait(false);
                }
            }
        }

        string GetInstallationUserName()
        {
            string username;
            return settings.TryGet("Installers.UserName", out username)
                ? username
                : WindowsIdentity.GetCurrent().Name;
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
            var errorQueue = ErrorQueueSettings.GetConfiguredErrorQueue(settings);
            var dequeueLimitations = GeDequeueLimitationsForReceivePipeline();
            var requiredTransactionSupport = settings.GetRequiredTransactionModeForReceives();

            var mainPipelineInstance = new Pipeline<ITransportReceiveContext>(builder, settings, pipelineConfiguration.MainPipeline);

            var pushSettings = new PushSettings(settings.LocalAddress(), errorQueue, purgeOnStartup, requiredTransactionSupport);
            yield return BuildReceiver(mainPipelineInstance, "Main", pushSettings, dequeueLimitations, cache);

            if (settings.InstanceSpecificQueue() != null)
            {
                var sharedReceiverPushSettings = new PushSettings(settings.InstanceSpecificQueue(), errorQueue, purgeOnStartup, requiredTransactionSupport);
                yield return BuildReceiver(mainPipelineInstance, "Main", sharedReceiverPushSettings, dequeueLimitations, cache);
            }

            foreach (var satellitePipeline in pipelineConfiguration.SatellitePipelines)
            {
                var satellitePushSettings = new PushSettings(satellitePipeline.ReceiveAddress, errorQueue, purgeOnStartup, satellitePipeline.RequiredTransportTransactionMode);
                var satellitePipelineInstance = new Pipeline<ITransportReceiveContext>(builder, settings, satellitePipeline);
                yield return BuildReceiver(satellitePipelineInstance, satellitePipeline.Name, satellitePushSettings, satellitePipeline.RuntimeSettings, cache);
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

        TransportReceiver BuildReceiver(Pipeline<ITransportReceiveContext> pipelineInstance, string name, PushSettings pushSettings, PushRuntimeSettings runtimeSettings, IPipelineCache cache)
        {
            var receiver = new TransportReceiver(
                name,
                builder,
                builder.Build<IPushMessages>(),
                pushSettings,
                pipelineInstance,
                cache,
                runtimeSettings,
                eventAggregator);

            return receiver;
        }

        IBuilder builder;
        FeatureActivator featureActivator;
        PipelineConfiguration pipelineConfiguration;
        SettingsHolder settings;
        IEventAggregator eventAggregator;
        TransportInfrastructure transportInfrastructure;
    }
}