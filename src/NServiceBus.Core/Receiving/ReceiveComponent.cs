namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Logging;
    using ObjectBuilder;
    using Outbox;
    using Persistence;
    using Pipeline;
    using Transport;
    using Unicast;

    partial class ReceiveComponent
    {
        ReceiveComponent(Configuration configuration,
            CriticalError criticalError,
            string errorQueue,
            TransportReceiveInfrastructure transportReceiveInfrastructure,
            RateLimitConfiguration systemOutageConfiguration)
        {
            this.configuration = configuration;
            this.criticalError = criticalError;
            this.errorQueue = errorQueue;
            this.transportReceiveInfrastructure = transportReceiveInfrastructure;
            this.systemOutageConfiguration = systemOutageConfiguration;
        }

        public static ReceiveComponent Initialize(
            Configuration configuration,
            string errorQueue,
            HostingComponent.Configuration hostingConfiguration,
            PipelineSettings pipelineSettings)
        {
            TransportReceiveInfrastructure transportReceiveInfrastructure = null;

            if (!configuration.IsSendOnlyEndpoint)
            {
                transportReceiveInfrastructure = configuration.transportSeam.TransportInfrastructure.ConfigureReceiveInfrastructure();

                if (configuration.CreateQueues)
                {
                    hostingConfiguration.AddInstaller(identity =>
                    {
                        var queueCreator = transportReceiveInfrastructure.QueueCreatorFactory();
                        return queueCreator.CreateQueueIfNecessary(configuration.transportSeam.QueueBindings, identity);
                    });
                }
            }

            var systemOutageConfig = pipelineSettings.Settings.Get<RateLimitConfiguration>();

            var receiveComponent = new ReceiveComponent(
                configuration,
                hostingConfiguration.CriticalError,
                errorQueue,
                transportReceiveInfrastructure,
                systemOutageConfig);

            if (configuration.IsSendOnlyEndpoint)
            {
                return receiveComponent;
            }

            receiveComponent.BindQueues(configuration.transportSeam.QueueBindings);

            pipelineSettings.Register("TransportReceiveToPhysicalMessageProcessingConnector", b =>
            {
                var storage = hostingConfiguration.Container.HasComponent<IOutboxStorage>() ? b.Build<IOutboxStorage>() : new NoOpOutboxStorage();
                return new TransportReceiveToPhysicalMessageConnector(storage);
            }, "Allows to abort processing the message");

            pipelineSettings.Register("LoadHandlersConnector", b =>
            {
                var adapter = hostingConfiguration.Container.HasComponent<ISynchronizedStorageAdapter>() ? b.Build<ISynchronizedStorageAdapter>() : new NoOpSynchronizedStorageAdapter();
                var syncStorage = hostingConfiguration.Container.HasComponent<ISynchronizedStorage>() ? b.Build<ISynchronizedStorage>() : new NoOpSynchronizedStorage();

                return new LoadHandlersConnector(b.Build<MessageHandlerRegistry>(), syncStorage, adapter);
            }, "Gets all the handlers to invoke from the MessageHandler registry based on the message type.");

            pipelineSettings.Register("ExecuteUnitOfWork", new UnitOfWorkBehavior(), "Executes the UoW");

            pipelineSettings.Register("InvokeHandlers", new InvokeHandlerTerminator(), "Calls the IHandleMessages<T>.Handle(T)");

            var externalHandlerRegistryUsed = hostingConfiguration.Container.HasComponent<MessageHandlerRegistry>();
            var handlerDiagnostics = new Dictionary<string, List<string>>();

            if (!externalHandlerRegistryUsed)
            {
                var messageHandlerRegistry = configuration.messageHandlerRegistry;

                RegisterMessageHandlers(messageHandlerRegistry, configuration.ExecuteTheseHandlersFirst, hostingConfiguration.Container, hostingConfiguration.AvailableTypes);

                foreach (var messageType in messageHandlerRegistry.GetMessageTypes())
                {
                    handlerDiagnostics[messageType.FullName] = messageHandlerRegistry.GetHandlersFor(messageType)
                        .Select(handler => handler.HandlerType.FullName)
                        .ToList();
                }
            }

            hostingConfiguration.AddStartupDiagnosticsSection("Receiving", new
            {
                configuration.LocalAddress,
                configuration.InstanceSpecificQueue,
                configuration.LogicalAddress,
                configuration.PurgeOnStartup,
                configuration.QueueNameBase,
                TransactionMode = configuration.TransactionMode.ToString("G"),
                configuration.PushRuntimeSettings.MaxConcurrency,
                Satellites = configuration.SatelliteDefinitions.Select(s => new
                {
                    s.Name,
                    s.ReceiveAddress,
                    TransactionMode = s.RequiredTransportTransactionMode.ToString("G"),
                    s.RuntimeSettings.MaxConcurrency
                }).ToArray(),
                ExternalHandlerRegistry = externalHandlerRegistryUsed,
                MessageHandlers = handlerDiagnostics
            });

            return receiveComponent;
        }

        static TransportTransactionMode GetRequiredTransactionMode(Settings settings, TransportInfrastructure transportInfrastructure)
        {
            var transportTransactionSupport = transportInfrastructure.TransactionMode;

            //if user haven't asked for a explicit level use what the transport supports
            if (!settings.UserHasProvidedTransportTransactionMode)
            {
                return transportTransactionSupport;
            }

            var requestedTransportTransactionMode = settings.UserTransportTransactionMode;

            if (requestedTransportTransactionMode > transportTransactionSupport)
            {
                throw new Exception($"Requested transaction mode `{requestedTransportTransactionMode}` can't be satisfied since the transport only supports `{transportTransactionSupport}`");
            }

            return requestedTransportTransactionMode;
        }

        public async Task PrepareToStart(IBuilder builder,
            RecoverabilityComponent recoverabilityComponent,
            MessageOperations messageOperations,
            PipelineComponent pipelineComponent,
            IPipelineCache pipelineCache)
        {
            if (configuration.IsSendOnlyEndpoint)
            {
                return;
            }

            var receivePipeline = pipelineComponent.CreatePipeline<ITransportReceiveContext>(builder);

            var consecutiveFailuresCircuitBreaker = new ConsecutiveFailuresCircuitBreaker("System outage circuit breaker", systemOutageConfiguration.NumberOfConsecutiveFailuresBeforeRateLimit, SwitchToRateLimitMode, SwitchBackToNormalMode, systemOutageConfiguration.WaitPeriodBetweenAttempts);

            mainPipelineExecutor = new MainPipelineExecutor(builder, pipelineCache, messageOperations, configuration.PipelineCompletedSubscribers, receivePipeline, consecutiveFailuresCircuitBreaker);

            if (configuration.PurgeOnStartup)
            {
                Logger.Warn("All queues owned by the endpoint will be purged on startup.");
            }

            AddReceivers(builder, recoverabilityComponent.GetRecoverabilityExecutorFactory(builder), transportReceiveInfrastructure.MessagePumpFactory);

            foreach (var receiver in receivers)
            {
                try
                {
                    await receiver.Init().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Logger.Fatal($"Receiver {receiver.Id} failed to initialize.", ex);
                    throw;
                }
            }
        }

        public async Task ReceivePreStartupChecks()
        {
            if (transportReceiveInfrastructure != null)
            {
                var result = await transportReceiveInfrastructure.PreStartupCheck().ConfigureAwait(false);

                if (!result.Succeeded)
                {
                    throw new Exception($"Pre start-up check failed: {result.ErrorMessage}");
                }
            }
        }

        public async Task Start()
        {
            foreach (var receiver in receivers)
            {
                try
                {
                    await receiver.Start().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Logger.Fatal($"Receiver {receiver.Id} failed to start.", ex);
                    throw;
                }
            }

            rateLimitTask = RateLimitLoop(rateLimitLoopCancellationToken.Token);
        }

        public Task Stop()
        {
            var receiverStopTasks = receivers.Select(async receiver =>
            {
                Logger.DebugFormat("Stopping {0} receiver", receiver.Id);
                await receiver.Stop().ConfigureAwait(false);
                Logger.DebugFormat("Stopped {0} receiver", receiver.Id);
            });

            rateLimitLoopCancellationToken.Cancel();

            return Task.WhenAll(receiverStopTasks);
        }

        void BindQueues(QueueBindings queueBindings)
        {
            if (configuration.IsSendOnlyEndpoint)
            {
                return;
            }

            queueBindings.BindReceiving(configuration.LocalAddress);

            if (configuration.InstanceSpecificQueue != null)
            {
                queueBindings.BindReceiving(configuration.InstanceSpecificQueue);
            }

            foreach (var satellitePipeline in configuration.SatelliteDefinitions)
            {
                queueBindings.BindReceiving(satellitePipeline.ReceiveAddress);
            }
        }

        void AddReceivers(IBuilder builder, RecoverabilityExecutorFactory recoverabilityExecutorFactory, Func<IPushMessages> messagePumpFactory)
        {
            var requiredTransactionSupport = configuration.TransactionMode;

            var recoverabilityExecutor = recoverabilityExecutorFactory.CreateDefault(configuration.LocalAddress);
            var pushSettings = new PushSettings(configuration.LocalAddress, errorQueue, configuration.PurgeOnStartup, requiredTransactionSupport);
            var dequeueLimitations = configuration.PushRuntimeSettings;

            receivers.Add(new TransportReceiver(MainReceiverId, messagePumpFactory, pushSettings, dequeueLimitations, mainPipelineExecutor, recoverabilityExecutor, criticalError, systemOutageConfiguration.RateLimitStartedNotification, systemOutageConfiguration.RateLimitEndedNotification));

            if (configuration.InstanceSpecificQueue != null)
            {
                var instanceSpecificQueue = configuration.InstanceSpecificQueue;
                var instanceSpecificRecoverabilityExecutor = recoverabilityExecutorFactory.CreateDefault(instanceSpecificQueue);
                var sharedReceiverPushSettings = new PushSettings(instanceSpecificQueue, errorQueue, configuration.PurgeOnStartup, requiredTransactionSupport);

                receivers.Add(new TransportReceiver(MainReceiverId, messagePumpFactory, sharedReceiverPushSettings, dequeueLimitations, mainPipelineExecutor, instanceSpecificRecoverabilityExecutor, criticalError, systemOutageConfiguration.RateLimitStartedNotification, systemOutageConfiguration.RateLimitEndedNotification));
            }

            foreach (var satellitePipeline in configuration.SatelliteDefinitions)
            {
                var satelliteRecoverabilityExecutor = recoverabilityExecutorFactory.Create(satellitePipeline.RecoverabilityPolicy, satellitePipeline.ReceiveAddress);
                var satellitePushSettings = new PushSettings(satellitePipeline.ReceiveAddress, errorQueue, configuration.PurgeOnStartup, satellitePipeline.RequiredTransportTransactionMode);

                receivers.Add(new TransportReceiver(satellitePipeline.Name, messagePumpFactory, satellitePushSettings, satellitePipeline.RuntimeSettings, new SatellitePipelineExecutor(builder, satellitePipeline), satelliteRecoverabilityExecutor, criticalError, systemOutageConfiguration.RateLimitStartedNotification, systemOutageConfiguration.RateLimitEndedNotification));
            }
        }

        async Task RateLimitLoop(CancellationToken cancellationToken)
        {
            //We want all the pumps to be running all the time in the desired mode until we call stop
            //We want to make sure that StopRateLimit signal is not lost
            //The circuit breaker ensures that if StartRateLimit has been called that eventually StopRateLimit is going to be called unless the endpoint stop

            while (true)
            {
                await transitionBetweenRateLimitModes.WaitAsync(cancellationToken).ConfigureAwait(false);
                var startRateLimiting = endpointShouldBeRateLimited;

                while (true)
                {
                    try
                    {
                        foreach (var receiver in receivers)
                        {
                            if (startRateLimiting)
                            {
                                await receiver.StartRateLimiting().ConfigureAwait(false);
                            }
                            else
                            {
                                await receiver.StopRateLimiting().ConfigureAwait(false);
                            }
                        }

                        break;
                    }
                    catch (Exception exception)
                    {
                        Logger.WarnFormat("Could not switch to {0} mode. '{1}'", startRateLimiting ? "rate limit" : "normal", exception);
                        //Raise critical error
                    }
                }
            }
        }

        void SwitchToRateLimitMode(Exception exception, long stateChangeTime)
        {
            if (stateChangeTime >= Interlocked.Read(ref lastStateChangeTime))
            {
                endpointShouldBeRateLimited = true;
                transitionBetweenRateLimitModes.Set();

                Interlocked.Exchange(ref lastStateChangeTime, stateChangeTime);
            }
        }

        void SwitchBackToNormalMode(long stateChangeTime)
        {
            endpointShouldBeRateLimited = false;
            transitionBetweenRateLimitModes.Set();

            Interlocked.Exchange(ref lastStateChangeTime, stateChangeTime);
        }

        static void RegisterMessageHandlers(MessageHandlerRegistry handlerRegistry, List<Type> orderedTypes, IConfigureComponents container, ICollection<Type> availableTypes)
        {
            var types = new List<Type>(availableTypes);

            foreach (var t in orderedTypes)
            {
                types.Remove(t);
            }

            types.InsertRange(0, orderedTypes);

            foreach (var t in types.Where(IsMessageHandler))
            {
                container.ConfigureComponent(t, DependencyLifecycle.InstancePerUnitOfWork);
                handlerRegistry.RegisterHandler(t);
            }

            container.RegisterSingleton(handlerRegistry);
        }

        public static bool IsMessageHandler(Type type)
        {
            if (type.IsAbstract || type.IsGenericTypeDefinition)
            {
                return false;
            }

            return type.GetInterfaces()
                .Where(@interface => @interface.IsGenericType)
                .Select(@interface => @interface.GetGenericTypeDefinition())
                .Any(genericTypeDef => genericTypeDef == IHandleMessagesType);
        }

        readonly AsyncAutoResetEvent transitionBetweenRateLimitModes = new AsyncAutoResetEvent();
        readonly TransportReceiveInfrastructure transportReceiveInfrastructure;
        readonly RateLimitConfiguration systemOutageConfiguration;
        readonly CancellationTokenSource rateLimitLoopCancellationToken = new CancellationTokenSource();
        Configuration configuration;
        List<TransportReceiver> receivers = new List<TransportReceiver>();
        IPipelineExecutor mainPipelineExecutor;
        CriticalError criticalError;
        string errorQueue;
        bool endpointShouldBeRateLimited;
        long lastStateChangeTime = 0;

        const string MainReceiverId = "Main";

        static Type IHandleMessagesType = typeof(IHandleMessages<>);
        static ILog Logger = LogManager.GetLogger<ReceiveComponent>();
        Task rateLimitTask;
    }
}