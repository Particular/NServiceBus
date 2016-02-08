namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Security.Principal;
    using System.Threading.Tasks;
    using NServiceBus.Config;
    using NServiceBus.ConsistencyGuarantees;
    using NServiceBus.Features;
    using NServiceBus.Installation;
    using NServiceBus.ObjectBuilder;
    using NServiceBus.Pipeline;
    using NServiceBus.Settings;
    using NServiceBus.Transports;

    class StartableEndpoint : IStartableEndpoint
    {
        SettingsHolder settings;
        IBuilder builder;
        FeatureActivator featureActivator;
        PipelineConfiguration pipelineConfiguration;
        IReadOnlyCollection<IWantToRunWhenBusStartsAndStops> startables;

        public StartableEndpoint(SettingsHolder settings, IBuilder builder, FeatureActivator featureActivator, PipelineConfiguration pipelineConfiguration, IReadOnlyCollection<IWantToRunWhenBusStartsAndStops> startables)
        {
            this.settings = settings;
            this.builder = builder;
            this.featureActivator = featureActivator;
            this.pipelineConfiguration = pipelineConfiguration;
            this.startables = startables;
        }

        public async Task<IEndpointInstance> Start()
        {
            DetectThrottlingConfig();

            var pipelineCache = new PipelineCache(builder, settings);
            var messageSession = CreateMessageSession(builder, pipelineCache);

            await RunInstallers().ConfigureAwait(false);
            var featureRunner = await StartFeatures(messageSession).ConfigureAwait(false);
            var runner = await StartStartables(messageSession).ConfigureAwait(false);

            AppDomain.CurrentDomain.SetPrincipalPolicy(PrincipalPolicy.WindowsPrincipal);

            var pipelineCollection = CreateIncomingPipelines(pipelineCache);

            var runningInstance = new RunningEndpointInstance(settings, builder, pipelineCollection, runner, featureRunner, messageSession);

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

        async Task<StartAndStoppablesRunner> StartStartables(IMessageSession session)
        {
            var allStartables = builder.BuildAll<IWantToRunWhenBusStartsAndStops>().Concat(startables).ToList();
            var runner = new StartAndStoppablesRunner(allStartables);
            await runner.Start(session).ConfigureAwait(false);
            return runner;
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
                throw new NotSupportedException($"Message throughput throttling has been removed. Please remove the '{nameof(TransportConfig.MaximumMessageThroughputPerSecond)}' attribute from the '{nameof(TransportConfig)}' configuration section and consult the documentation for further information.");
            }
        }

        static IMessageSession CreateMessageSession(IBuilder builder, IPipelineCache cache)
        {
            var rootContext = new RootContext(builder, cache);
            return new MessageSession(rootContext);
        }

        IEnumerable<TransportReceiver> BuildPipelines(IPipelineCache cache)
        {
            var errorQueue = ErrorQueueSettings.GetConfiguredErrorQueue(settings);

            var dequeueLimitations = GeDequeueLimitationsForReceivePipeline();
            var requiredTransactionSupport = settings.GetRequiredTransactionModeForReceives();

            var pushSettings = new PushSettings(settings.LocalAddress(), errorQueue, settings.GetOrDefault<bool>("Transport.PurgeOnStartup"), requiredTransactionSupport);

            yield return BuildPipelineInstance(pipelineConfiguration.MainPipeline, "Main", pushSettings, dequeueLimitations, cache);

            foreach (var satellitePipeline in pipelineConfiguration.SatellitePipelines)
            {
                var satellitePushSettings = new PushSettings(satellitePipeline.ReceiveAddress, errorQueue, settings.GetOrDefault<bool>("Transport.PurgeOnStartup"), satellitePipeline.RequiredTransportTransactionMode);

                yield return BuildPipelineInstance(satellitePipeline, satellitePipeline.Name, satellitePushSettings, satellitePipeline.RuntimeSettings, cache);
            }
        }

        //note: this should be handled in a feature but we don't have a good
        // extension point to plugin atm
        PushRuntimeSettings GeDequeueLimitationsForReceivePipeline()
        {
            var transportConfig = settings.GetConfigSection<TransportConfig>();

            if (transportConfig != null && transportConfig.MaximumConcurrencyLevel != 0)
            {
                throw new NotSupportedException($"The TransportConfig.MaximumConcurrencyLevel has been removed. Please remove the '{nameof(TransportConfig.MaximumMessageThroughputPerSecond)}' attribute from the '{nameof(TransportConfig)}' configuration section and use 'EndpointConfiguration.LimitMessageProcessingConcurrencyTo' instead.");
            }

            MessageProcessingOptimizationExtensions.ConcurrencyLimit concurrencyLimit;

            if (settings.TryGet(out concurrencyLimit))
            {
                return new PushRuntimeSettings(concurrencyLimit.MaxValue);
            }

            return PushRuntimeSettings.Default;
        }

        TransportReceiver BuildPipelineInstance(PipelineModifications modifications, string name, PushSettings pushSettings, PushRuntimeSettings runtimeSettings, IPipelineCache cache)
        {
            var pipelineInstance = new Pipeline<ITransportReceiveContext>(builder, settings, modifications);
            var receiver = new TransportReceiver(
                name,
                builder,
                builder.Build<IPushMessages>(),
                pushSettings,
                pipelineInstance,
                cache,
                runtimeSettings);

            return receiver;
        }
    }
}