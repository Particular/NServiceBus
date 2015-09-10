namespace NServiceBus.Unicast
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Principal;
    using System.Threading;
    using System.Threading.Tasks;
    using NServiceBus.Config;
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
            CriticalError criticalError,
            IMessageMapper messageMapper,
            IBuilder builder,
            ReadOnlySettings settings,
            IDispatchMessages dispatcher)
        {
            this.criticalError = criticalError;
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

            lock (startLocker)
            {
                if (started)
                {
                    return this;
                }

                AppDomain.CurrentDomain.SetPrincipalPolicy(PrincipalPolicy.WindowsPrincipal);
                var pipelines = BuildPipelines().ToArray();

                pipelineCollection = new PipelineCollection(pipelines);
                pipelineCollection.Start().GetAwaiter().GetResult();

                started = true;
            }

            ProcessStartupItems(
                builder.BuildAll<IWantToRunWhenBusStartsAndStops>().ToList(),
                toRun =>
                {
                    toRun.Start();
                    thingsRanAtStartup.Add(toRun);
                    Log.DebugFormat("Started {0}.", toRun.GetType().AssemblyQualifiedName);
                },
                ex => criticalError.Raise("Startup task failed to complete.", ex),
                startCompletedEvent);

            return this;
        }

        IEnumerable<TransportReceiver> BuildPipelines()
        {
            var errorQueue = ErrorQueueSettings.GetConfiguredErrorQueue(settings);
            var pipelinesCollection = settings.Get<PipelineConfiguration>();

            var dequeueLimitations = GeDequeueLimitationsForReceivePipeline();

            var transactionSettings = new Transport.TransactionSettings(settings);
            var pushSettings = new PushSettings(settings.LocalAddress(), errorQueue, settings.GetOrDefault<bool>("Transport.PurgeOnStartup"), transactionSettings);


            yield return BuildPipelineInstance(pipelinesCollection.MainPipeline, "Main", pushSettings, dequeueLimitations);

            foreach (var satellitePipeline in pipelinesCollection.SatellitePipelines)
            {
                var satellitePushSettings = new PushSettings(satellitePipeline.ReceiveAddress, errorQueue, settings.GetOrDefault<bool>("Transport.PurgeOnStartup"),satellitePipeline.TransactionSettings ?? transactionSettings);

                yield return BuildPipelineInstance(satellitePipeline, satellitePipeline.Name, satellitePushSettings, satellitePipeline.PushRuntimeSettings ?? PushRuntimeSettings.Default);
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

        void ExecuteIWantToRunAtStartupStopMethods()
        {
            // Ensuring IWantToRunWhenBusStartsAndStops.Start has been called.
            startCompletedEvent.WaitOne();

            var tasksToStop = Interlocked.Exchange(ref thingsRanAtStartup, new ConcurrentBag<IWantToRunWhenBusStartsAndStops>());
            if (!tasksToStop.Any())
            {
                return;
            }

            ProcessStartupItems(
                tasksToStop,
                toRun =>
                {
                    toRun.Stop();
                    Log.DebugFormat("Stopped {0}.", toRun.GetType().AssemblyQualifiedName);
                },
                ex => Log.Fatal("Startup task failed to stop.", ex),
                stopCompletedEvent);

            stopCompletedEvent.WaitOne();
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

            ExecuteIWantToRunAtStartupStopMethods();

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
        object startLocker = new object();

        static ILog Log = LogManager.GetLogger<UnicastBus>();

        ConcurrentBag<IWantToRunWhenBusStartsAndStops> thingsRanAtStartup = new ConcurrentBag<IWantToRunWhenBusStartsAndStops>();
        ManualResetEvent startCompletedEvent = new ManualResetEvent(false);
        ManualResetEvent stopCompletedEvent = new ManualResetEvent(true);

        PipelineCollection pipelineCollection;
        ContextualBus busImpl;
        CriticalError criticalError;
        ReadOnlySettings settings;
        IBuilder builder;


        static void ProcessStartupItems<T>(IEnumerable<T> items, Action<T> iteration, Action<Exception> inCaseOfFault, EventWaitHandle eventToSet)
        {
            eventToSet.Reset();

            Task.Factory.StartNew(() =>
            {
                Parallel.ForEach(items, iteration);
                eventToSet.Set();
            }, TaskCreationOptions.LongRunning | TaskCreationOptions.PreferFairness)
            .ContinueWith(task =>
            {
                eventToSet.Set();
                inCaseOfFault(task.Exception);
            }, TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.LongRunning);
        }
    }
}
