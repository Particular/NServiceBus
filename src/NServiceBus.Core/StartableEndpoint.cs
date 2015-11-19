namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Principal;
    using System.Threading.Tasks;
    using NServiceBus.Config;
    using NServiceBus.ConsistencyGuarantees;
    using NServiceBus.Faults;
    using NServiceBus.Features;
    using NServiceBus.ObjectBuilder;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;
    using NServiceBus.Settings;
    using NServiceBus.Transport;
    using NServiceBus.Transports;
    using NServiceBus.Unicast;

    class StartableEndpoint : IStartableEndpoint
    {
        SettingsHolder settings;
        readonly IBuilder builder;
        readonly FeatureActivator featureActivator;
        PipelineConfiguration pipelineConfiguration;
        readonly IReadOnlyCollection<IWantToRunWhenBusStartsAndStops> startables;

        public StartableEndpoint(SettingsHolder settings, IBuilder builder, FeatureActivator featureActivator, PipelineConfiguration pipelineConfiguration, IReadOnlyCollection<IWantToRunWhenBusStartsAndStops> startables)
        {
            this.settings = settings;
            this.builder = builder;
            this.featureActivator = featureActivator;
            this.pipelineConfiguration = pipelineConfiguration;
            this.startables = startables;
        }

        public async Task<IEndpoint> Start()
        {
            var busInterface = new StartUpBusInterface(builder);
            var featureRunner = new FeatureRunner(builder, featureActivator);
            var busContext = busInterface.CreateBusContext();
            await featureRunner.Start(busContext).ConfigureAwait(false);

            var allStartables = builder.BuildAll<IWantToRunWhenBusStartsAndStops>().Concat(startables).ToList();
            var runner = new StartAndStoppablesRunner(allStartables);
            await runner.Start(busContext).ConfigureAwait(false);

            AppDomain.CurrentDomain.SetPrincipalPolicy(PrincipalPolicy.WindowsPrincipal);

            PipelineCollection pipelineCollection;
            if (settings.GetOrDefault<bool>("Endpoint.SendOnly"))
            {
                pipelineCollection = new PipelineCollection(Enumerable.Empty<TransportReceiver>());
            }
            else
            {
                var pipelines = BuildPipelines().ToArray();
                pipelineCollection = new PipelineCollection(pipelines);
                await pipelineCollection.Start().ConfigureAwait(false);
            }
            var runningInstance = new RunningEndpoint(builder, pipelineCollection, runner, featureRunner, busInterface);
            return runningInstance;
        }

        class StartUpBusInterface : IBusInterface
        {
            IBuilder builder;

            public StartUpBusInterface(IBuilder builder)
            {
                this.builder = builder;
            }

            public IBusContext CreateBusContext()
            {
                var rootContext = new RootContext(builder);
                return new BusContext(rootContext);
            }
        }

        IEnumerable<TransportReceiver> BuildPipelines()
        {
            var errorQueue = ErrorQueueSettings.GetConfiguredErrorQueue(settings);

            var dequeueLimitations = GeDequeueLimitationsForReceivePipeline();
            var requiredTransactionSupport = settings.GetRequiredTransactionSupportForReceives();

            var pushSettings = new PushSettings(settings.LocalAddress(), errorQueue, settings.GetOrDefault<bool>("Transport.PurgeOnStartup"), requiredTransactionSupport);

            yield return BuildPipelineInstance(pipelineConfiguration.MainPipeline, "Main", pushSettings, dequeueLimitations);

            foreach (var satellitePipeline in pipelineConfiguration.SatellitePipelines)
            {
                var satellitePushSettings = new PushSettings(satellitePipeline.ReceiveAddress, errorQueue, settings.GetOrDefault<bool>("Transport.PurgeOnStartup"), satellitePipeline.RequiredTransactionSupport);

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
            var pipelineInstance = new PipelineBase<TransportReceiveContext>(builder, settings, modifications);
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