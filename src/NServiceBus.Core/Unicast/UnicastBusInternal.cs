namespace NServiceBus.Unicast
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Principal;
    using System.Threading;
    using System.Threading.Tasks;
    using NServiceBus.Features;
    using NServiceBus.Licensing;
    using NServiceBus.Logging;
    using NServiceBus.MessageInterfaces;
    using NServiceBus.ObjectBuilder;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;
    using NServiceBus.Settings;
    using NServiceBus.Transports;
    using NServiceBus.Unicast.Routing;
    using NServiceBus.Unicast.Transport;

    /// <summary>
    /// A unicast implementation of <see cref="IBus"/> for NServiceBus.
    /// </summary>
    partial class UnicastBusInternal : IStartableBus
    {
        /// <summary>
        /// Initializes a new instance of <see cref="UnicastBus"/>.
        /// </summary>
        internal UnicastBusInternal(
            BehaviorContextStacker contextStacker,
            IExecutor executor,
            CriticalError criticalError,
            IMessageMapper messageMapper, 
            IBuilder builder, 
            Configure configure, 
            IManageSubscriptions subscriptionManager, 
            ReadOnlySettings settings,
            TransportDefinition transportDefinition,
            IDispatchMessages messageSender,
            StaticMessageRouter messageRouter)
        {
            this.executor = executor;
            this.criticalError = criticalError;
            this.settings = settings;
            this.builder = builder;

            busImpl = new ContextualBus( 
                contextStacker,
                messageMapper, 
                builder, 
                configure,
                subscriptionManager, 
                settings,
                transportDefinition,
                messageSender,
                messageRouter);
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
                executor.Start(pipelines.Select(x => x.Id).ToArray());

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
            var pipelinesCollection = settings.Get<PipelineConfiguration>();

            yield return BuildPipelineInstance(pipelinesCollection.MainPipeline, pipelinesCollection.ReceiveBehavior, "Main", settings.LocalAddress());

            foreach (var satellite in pipelinesCollection.SatellitePipelines)
            {
                yield return BuildPipelineInstance(satellite, pipelinesCollection.ReceiveBehavior, satellite.Name, satellite.ReceiveAddress);
            }
        }

        TransportReceiver BuildPipelineInstance(PipelineModifications modifications, RegisterStep receiveBehavior, string name, string address)
        {
            var dequeueSettings = new DequeueSettings(address, settings.GetOrDefault<bool>("Transport.PurgeOnStartup"));

            var pipelineInstance = new PipelineBase<IncomingContext>(builder, settings, modifications, receiveBehavior);
            var receiver = new TransportReceiver(
                name,
                builder,
                builder.Build<IDequeueMessages>(),
                dequeueSettings,
                pipelineInstance,
                executor);
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
            executor.Stop();
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
        IExecutor executor;
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
