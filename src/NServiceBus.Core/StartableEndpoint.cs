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
    using NServiceBus.Pipeline.Contexts;
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
            var busInterface = new StartUpBusInterface(builder);
            var busSession = busInterface.CreateBusSession();

            await RunInstallers().ConfigureAwait(false);
            var featureRunner = await StartFeatures(busSession).ConfigureAwait(false);
            var runner = await StartStartables(busSession).ConfigureAwait(false);

            AppDomain.CurrentDomain.SetPrincipalPolicy(PrincipalPolicy.WindowsPrincipal);

            var pipelineCollection = CreateIncomingPipelines();

            var runningInstance = new RunningEndpointInstance(builder, pipelineCollection, runner, featureRunner, busInterface);

            // set the started endpoint on CriticalError to pass the endpoint to the critical error action
            builder.Build<CriticalError>().Endpoint = runningInstance;

            await StartPipelines(pipelineCollection).ConfigureAwait(false);

            return runningInstance;
        }

        static Task StartPipelines(PipelineCollection pipelineCollection)
        {
            return pipelineCollection.Start();
        }

        PipelineCollection CreateIncomingPipelines()
        {
            PipelineCollection pipelineCollection;
            if (settings.GetOrDefault<bool>("Endpoint.SendOnly"))
            {
                pipelineCollection = new PipelineCollection(Enumerable.Empty<TransportReceiver>());
            }
            else
            {
                var pipelines = BuildPipelines().ToArray();
                pipelineCollection = new PipelineCollection(pipelines);
            }
            return pipelineCollection;
        }

        async Task<StartAndStoppablesRunner> StartStartables(IBusSession session)
        {
            var allStartables = builder.BuildAll<IWantToRunWhenBusStartsAndStops>().Concat(startables).ToList();
            var runner = new StartAndStoppablesRunner(allStartables);
            await runner.Start(session).ConfigureAwait(false);
            return runner;
        }

        async Task<FeatureRunner> StartFeatures(IBusSession session)
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

        class StartUpBusInterface : IBusSessionFactory
        {
            IBuilder builder;

            public StartUpBusInterface(IBuilder builder)
            {
                this.builder = builder;
            }

            public IBusSession CreateBusSession()
            {
                var rootContext = new RootContext(builder);
                return new BusSession(rootContext);
            }
        }

        IEnumerable<TransportReceiver> BuildPipelines()
        {
            var errorQueue = ErrorQueueSettings.GetConfiguredErrorQueue(settings);

            var dequeueLimitations = GeDequeueLimitationsForReceivePipeline();
            var requiredTransactionSupport = settings.GetRequiredTransactionModeForReceives();

            var pushSettings = new PushSettings(settings.LocalAddress(), errorQueue, settings.GetOrDefault<bool>("Transport.PurgeOnStartup"), requiredTransactionSupport);

            yield return BuildPipelineInstance(pipelineConfiguration.MainPipeline, "Main", pushSettings, dequeueLimitations);

            foreach (var satellitePipeline in pipelineConfiguration.SatellitePipelines)
            {
                var satellitePushSettings = new PushSettings(satellitePipeline.ReceiveAddress, errorQueue, settings.GetOrDefault<bool>("Transport.PurgeOnStartup"), satellitePipeline.RequiredTransportTransactionMode);

                yield return BuildPipelineInstance(satellitePipeline, satellitePipeline.Name, satellitePushSettings, satellitePipeline.RuntimeSettings);
            }
        }

        //note: this should be handled in a feature but we don't have a good
        // extension point to plugin atm
        PushRuntimeSettings GeDequeueLimitationsForReceivePipeline()
        {
            var transportConfig = settings.GetConfigSection<TransportConfig>();

            int? concurrencyMaxFromConfig = null;

            if (transportConfig != null && transportConfig.MaximumConcurrencyLevel > 0)
            {
                concurrencyMaxFromConfig = transportConfig.MaximumConcurrencyLevel;
            }

            MessageProcessingOptimizationExtensions.ConcurrencyLimit concurrencyLimit;

            if (settings.TryGet(out concurrencyLimit))
            {
                if (concurrencyMaxFromConfig.HasValue)
                {
                    throw new Exception("Max receive concurrency specified both via API and configuration, please remove one of them.");
                }

                return new PushRuntimeSettings(concurrencyLimit.MaxValue);
            }

            if (concurrencyMaxFromConfig.HasValue)
            {
                return new PushRuntimeSettings(concurrencyMaxFromConfig.Value);
            }

            return PushRuntimeSettings.Default;
        }

        TransportReceiver BuildPipelineInstance(PipelineModifications modifications, string name, PushSettings pushSettings, PushRuntimeSettings runtimeSettings)
        {
            var pipelineInstance = new PipelineBase<ITransportReceiveContext>(builder, settings, modifications);
            var receiver = new TransportReceiver(
                name,
                builder,
                builder.Build<IPushMessages>(),
                pushSettings,
                pipelineInstance,
                runtimeSettings);

            return receiver;
        }
    }
}