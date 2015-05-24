namespace NServiceBus.Unicast
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Security.Principal;
    using System.Threading;
    using System.Threading.Tasks;
    using Hosting;
    using Licensing;
    using Logging;
    using NServiceBus.Features;
    using NServiceBus.MessageInterfaces;
    using NServiceBus.Pipeline.Contexts;
    using NServiceBus.Transports;
    using NServiceBus.Unicast.Messages;
    using NServiceBus.Unicast.Routing;
    using NServiceBus.Unicast.Transport;
    using ObjectBuilder;
    using Pipeline;
    using Settings;

    interface IRealBus
    {
    }

    /// <summary>
    /// A unicast implementation of <see cref="IBus"/> for NServiceBus.
    /// </summary>
    public partial class UnicastBus : IStartableBus, IRealBus
    {
        /// <summary>
        /// Initializes a new instance of <see cref="UnicastBus"/>.
        /// </summary>
        public UnicastBus(
            BehaviorContext rootContext,
            IExecutor executor,
            CriticalError criticalError,
            IMessageMapper messageMapper, 
            IBuilder builder, 
            Configure configure, 
            IManageSubscriptions subscriptionManager, 
            MessageMetadataRegistry messageMetadataRegistry,
            ReadOnlySettings settings,
            TransportDefinition transportDefinition,
            ISendMessages messageSender,
            StaticMessageRouter messageRouter,
            HostInformation hostInformation)
        {
            this.executor = executor;
            this.criticalError = criticalError;
            this.settings = settings;
            this.builder = builder;

            busImpl = new ContextualBus( 
                () => rootContext,
                messageMapper, 
                builder, 
                configure,
                subscriptionManager, 
                messageMetadataRegistry,
                settings,
                transportDefinition,
                messageSender,
                messageRouter,
                hostInformation);
        }

        /// <summary>
        /// Provides access to the current host information
        /// </summary>
        [ObsoleteEx(Message = "We have introduced a more explicit API to set the host identifier, see busConfiguration.UniquelyIdentifyRunningInstance()", TreatAsErrorFromVersion = "6", RemoveInVersion = "7")]
        public HostInformation HostInformation
        {
            // we should be making the getter and setter throw a NotImplementedException
            // but some containers try to inject on public properties
            get; set;
        }

        /// <summary>
        /// <see cref="IStartableBus.Start()"/>
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

            var wantToRunWhenBusStartsAndStops = Builder.BuildAll<IWantToRunWhenBusStartsAndStops>().ToList();
            var runWhenBusStartsAndStops = Builder.BuildAll<IRunWhenBusStartsAndStops>().ToList();

            var everyThingWhichNeedsToRun = wantToRunWhenBusStartsAndStops
                .Select(r => new StartAndStopAdapter(r))
                .Concat(runWhenBusStartsAndStops
                .Select(r => new StartAndStopAdapter(r))).ToList();

            ProcessStartupItems(
                everyThingWhichNeedsToRun,
                toRun =>
                {
                    toRun.Start(new RunContext(this));
                    thingsRanAtStartup.Add(toRun);
                    Log.DebugFormat("Started {0}.", toRun.Name);
                },
                ex => criticalError.Raise("Startup task failed to complete.", ex),
                startCompletedEvent);

            return this;
        }

        [DebuggerNonUserCode]
        class StartAndStopAdapter
        {
            readonly IWantToRunWhenBusStartsAndStops wantToRunWhenBusStartsAndStops;
            readonly IRunWhenBusStartsAndStops runWhenBusStartsAndStops;

            public StartAndStopAdapter(IWantToRunWhenBusStartsAndStops wantToRunWhenBusStartsAndStops)
            {
                this.wantToRunWhenBusStartsAndStops = wantToRunWhenBusStartsAndStops;
            }

            public StartAndStopAdapter(IRunWhenBusStartsAndStops runWhenBusStartsAndStops)
            {
                this.runWhenBusStartsAndStops = runWhenBusStartsAndStops;
            }

            public string Name
            {
                get
                {
                    if (wantToRunWhenBusStartsAndStops != null)
                    {
                        return wantToRunWhenBusStartsAndStops.GetType().AssemblyQualifiedName;
                    }

                    return runWhenBusStartsAndStops != null ? runWhenBusStartsAndStops.GetType().AssemblyQualifiedName : GetType().AssemblyQualifiedName;
                }
            }

            public void Start(IRunContext context)
            {
                if (wantToRunWhenBusStartsAndStops != null)
                {
                    wantToRunWhenBusStartsAndStops.Start();
                    return;
                }

                if (runWhenBusStartsAndStops != null)
                {
                    runWhenBusStartsAndStops.Start(context);
                }
            }

            public void Stop(IRunContext context)
            {
                if (wantToRunWhenBusStartsAndStops != null)
                {
                    wantToRunWhenBusStartsAndStops.Stop();
                    return;
                }

                if (runWhenBusStartsAndStops != null)
                {
                    runWhenBusStartsAndStops.Stop(context);
                }
            }
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

            var tasksToStop = Interlocked.Exchange(ref thingsRanAtStartup, new ConcurrentBag<StartAndStopAdapter>());
            if (!tasksToStop.Any())
            {
                return;
            }

            ProcessStartupItems(
                tasksToStop,
                toRun =>
                {
                    toRun.Stop(new RunContext(this));
                    Log.DebugFormat("Stopped {0}.", toRun.Name);
                },
                ex => Log.Fatal("Startup task failed to stop.", ex),
                stopCompletedEvent);

            stopCompletedEvent.WaitOne();
        }

        /// <summary>
        /// <see cref="IDisposable.Dispose"/>
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
            Builder.Dispose();
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
            var featureRunner = Builder.Build<FeatureRunner>();
            featureRunner.Stop();
        }


        volatile bool started;
        object startLocker = new object();

        static ILog Log = LogManager.GetLogger<UnicastBus>();

        ConcurrentBag<StartAndStopAdapter> thingsRanAtStartup = new ConcurrentBag<StartAndStopAdapter>();
        ManualResetEvent startCompletedEvent = new ManualResetEvent(false);
        ManualResetEvent stopCompletedEvent = new ManualResetEvent(true);

        PipelineCollection pipelineCollection;
        ContextualBus busImpl;
        ReadOnlySettings settings;
        IExecutor executor;
        CriticalError criticalError;
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


        /// <summary>
        /// Only for tests
        /// </summary>
        public ReadOnlySettings Settings
        {
            get { return settings; }
        }

        /// <summary>
        /// Only for tests
        /// </summary>
        public IBuilder Builder
        {
            get { return builder; }
        }

    }
}
