namespace NServiceBus.Unicast
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Principal;
    using System.Threading.Tasks;
    using Config;
    using ConsistencyGuarantees;
    using Faults;
    using Features;
    using Logging;
    using ObjectBuilder;
    using Pipeline;
    using Pipeline.Contexts;
    using Settings;
    using NServiceBus.Transport;
    using Transports;

    partial class UnicastBusInternal : IStartableBus
    {
        public UnicastBusInternal(IBuilder builder, ReadOnlySettings settings, StaticBus bus)
        {
            this.settings = settings;
            this.builder = builder;
            this.bus = bus;
            rootContext = new RootContext(builder);
        }

        public async Task<IBus> StartAsync()
        {
            if (started)
            {
                return this;
            }

            var startables = builder.BuildAll<IWantToRunWhenBusStartsAndStops>().ToList();
            runner = new StartAndStoppablesRunner(startables);
            await runner.StartAsync();

            AppDomain.CurrentDomain.SetPrincipalPolicy(PrincipalPolicy.WindowsPrincipal);
            var pipelines = BuildPipelines().ToArray();

            pipelineCollection = new PipelineCollection(pipelines);
            await pipelineCollection.Start();

            started = true;

            return this;
        }

        IEnumerable<TransportReceiver> BuildPipelines()
        {
            var errorQueue = ErrorQueueSettings.GetConfiguredErrorQueue(settings);
            var pipelinesCollection = settings.Get<PipelineConfiguration>();

            var dequeueLimitations = GeDequeueLimitationsForReceivePipeline();
            var requiredTransactionSupport = settings.GetRequiredTransactionSupportForReceives();

            var pushSettings = new PushSettings(settings.LocalAddress(), errorQueue, settings.GetOrDefault<bool>("Transport.PurgeOnStartup"), requiredTransactionSupport);


            yield return BuildPipelineInstance(pipelinesCollection.MainPipeline, "Main", pushSettings, dequeueLimitations);

            foreach (var satellitePipeline in pipelinesCollection.SatellitePipelines)
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

        public void Dispose()
        {
            //Injected at compile time
        }

        [Obsolete("", true)]
        public IMessageContext CurrentMessageContext
        {
            get { throw new NotImplementedException(); }
        }

        // ReSharper disable once UnusedMember.Local
        void DisposeManaged()
        {
            InnerShutdown();
            builder.Dispose();
        }

        void InnerShutdown()
        {
            StopFeatures();

            if (!started)
            {
                return;
            }

            Log.Info("Initiating shutdown.");
            pipelineCollection.Stop().GetAwaiter().GetResult();

            runner.StopAsync().GetAwaiter().GetResult();

            Log.Info("Shutdown complete.");

            started = false;
        }

        void StopFeatures()
        {
            // Pull the feature  runner singleton out of the container
            // features are always stopped
            var featureRunner = builder.Build<FeatureRunner>();
            featureRunner.Stop();
        }

        volatile bool started;

        static ILog Log = LogManager.GetLogger<UnicastBus>();

        StartAndStoppablesRunner runner;
        PipelineCollection pipelineCollection;
        ReadOnlySettings settings;
        IBuilder builder;
        RootContext rootContext;
        StaticBus bus;
    }
}