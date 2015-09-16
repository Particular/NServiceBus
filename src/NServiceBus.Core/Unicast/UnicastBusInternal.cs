namespace NServiceBus.Unicast
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Principal;
    using NServiceBus.Config;
    using NServiceBus.ConsistencyGuarantees;
    using NServiceBus.Faults;
    using NServiceBus.Features;
    using NServiceBus.Licensing;
    using NServiceBus.Logging;
    using NServiceBus.MessageInterfaces;
    using NServiceBus.ObjectBuilder;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;
    using NServiceBus.Settings;
    using NServiceBus.Transports;
    using NServiceBus.Unicast.Transport;

    /// <summary>
    /// A unicast implementation of <see cref="IBus"/> for NServiceBus.
    /// </summary>
    partial class UnicastBusInternal : IStartableBus
    {
        /// <summary>
        /// Initializes a new instance of <see cref="UnicastBus"/>.
        /// </summary>
        public UnicastBusInternal(
            BehaviorContextStacker contextStacker,
            IMessageMapper messageMapper,
            IBuilder builder,
            ReadOnlySettings settings,
            IDispatchMessages dispatcher)
        {
            this.settings = settings;
            this.builder = builder;

            busImpl = new ContextualBus(
                contextStacker,
                messageMapper,
                builder,
                settings,
                dispatcher);
        }

        /// <summary>
        /// <see cref="IStartableBus.Start()"/>.
        /// </summary>
        public IBus Start()
        {
            LicenseManager.PromptUserForLicenseIfTrialHasExpired();

            if (started)
            {
                return this;
            }

            var startables = builder.BuildAll<IWantToRunWhenBusStartsAndStops>().ToList();
            runner = new StartAndStoppablesRunner(startables);
            runner.StartAsync().GetAwaiter().GetResult();
            
            AppDomain.CurrentDomain.SetPrincipalPolicy(PrincipalPolicy.WindowsPrincipal);
            var pipelines = BuildPipelines().ToArray();

            pipelineCollection = new PipelineCollection(pipelines);
            pipelineCollection.Start().GetAwaiter().GetResult();

            started = true;

            return this;
        }

        IEnumerable<TransportReceiver> BuildPipelines()
        {
            var errorQueue = ErrorQueueSettings.GetConfiguredErrorQueue(settings);
            var pipelinesCollection = settings.Get<PipelineConfiguration>();

            var dequeueLimitations = GeDequeueLimitationsForReceivePipeline();
            var defaultConsistencyGuarantee = settings.GetConsistencyGuarantee();

            var pushSettings = new PushSettings(settings.LocalAddress(), errorQueue, settings.GetOrDefault<bool>("Transport.PurgeOnStartup"), defaultConsistencyGuarantee);


            yield return BuildPipelineInstance(pipelinesCollection.MainPipeline, "Main", pushSettings, dequeueLimitations);

            foreach (var satellitePipeline in pipelinesCollection.SatellitePipelines)
            {
                var satellitePushSettings = new PushSettings(satellitePipeline.ReceiveAddress, errorQueue, settings.GetOrDefault<bool>("Transport.PurgeOnStartup"), satellitePipeline.ConsistencyGuarantee);

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

        /// <summary>
        /// <see cref="IDisposable.Dispose"/>.
        /// </summary>
        public void Dispose()
        {
            //Injected at compile time
        }

        // ReSharper disable once UnusedMember.Local
        void DisposeManaged()
        {
            InnerShutdown();
            busImpl.Dispose();
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
        ContextualBus busImpl;
        ReadOnlySettings settings;
        IBuilder builder;
    }
}
