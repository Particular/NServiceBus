namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Logging;
    using ObjectBuilder;
    using Outbox;
    using Persistence;
    using Pipeline;
    using Settings;
    using Transport;
    using Unicast;

    class ReceiveComponent
    {
        ReceiveComponent(ReceiveConfiguration transportReceiveConfiguration,
            Func<IPushMessages> messagePumpFactory,
            PipelineComponent pipeline,
            IEventAggregator eventAggregator,
            CriticalError criticalError,
            string errorQueue)
        {
            this.transportReceiveConfiguration = transportReceiveConfiguration;
            this.messagePumpFactory = messagePumpFactory;
            this.pipeline = pipeline;
            this.eventAggregator = eventAggregator;
            this.criticalError = criticalError;
            this.errorQueue = errorQueue;
        }

        public static ReceiveComponent Initialize(Configuration configuration,
            ReceiveConfiguration transportReceiveConfiguration,
            TransportComponent transportComponent,
            PipelineComponent pipeline,
            EventAggregator eventAggregator,
            string errorQueue,
            HostingComponent hostingComponent,
            PipelineSettings pipelineSettings,
            ContainerComponent container)
        {
            Func<IPushMessages> messagePumpFactory = null;

            //we don't need the message pump factory for send-only endpoints
            if (transportReceiveConfiguration != null)
            {
                messagePumpFactory = transportComponent.GetMessagePumpFactory();
            }

            var receiveComponent = new ReceiveComponent(transportReceiveConfiguration,
                messagePumpFactory,
                pipeline,
                eventAggregator,
                hostingComponent.CriticalError,
                errorQueue);

            receiveComponent.BindQueues(transportComponent.QueueBindings);

            pipelineSettings.Register("TransportReceiveToPhysicalMessageProcessingConnector", b =>
            {
                var storage = container.ContainerConfiguration.HasComponent<IOutboxStorage>() ? b.Build<IOutboxStorage>() : new NoOpOutboxStorage();
                return new TransportReceiveToPhysicalMessageConnector(storage);
            }, "Allows to abort processing the message");

            pipelineSettings.Register("LoadHandlersConnector", b =>
            {
                var adapter = container.ContainerConfiguration.HasComponent<ISynchronizedStorageAdapter>() ? b.Build<ISynchronizedStorageAdapter>() : new NoOpSynchronizedStorageAdapter();
                var syncStorage = container.ContainerConfiguration.HasComponent<ISynchronizedStorage>() ? b.Build<ISynchronizedStorage>() : new NoOpSynchronizedStorage();

                return new LoadHandlersConnector(b.Build<MessageHandlerRegistry>(), syncStorage, adapter);
            }, "Gets all the handlers to invoke from the MessageHandler registry based on the message type.");

            pipelineSettings.Register("ExecuteUnitOfWork", new UnitOfWorkBehavior(), "Executes the UoW");

            pipelineSettings.Register("InvokeHandlers", new InvokeHandlerTerminator(), "Calls the IHandleMessages<T>.Handle(T)");

            if (!container.ContainerConfiguration.HasComponent<MessageHandlerRegistry>())
            {
                var orderedHandlers = configuration.ExecuteTheseHandlersFirst;

                LoadMessageHandlers(configuration, orderedHandlers, container.ContainerConfiguration, hostingComponent.AvailableTypes);
            }

            if (transportReceiveConfiguration != null)
            {
                hostingComponent.AddStartupDiagnosticsSection("Receiving", new
                {
                    transportReceiveConfiguration.LocalAddress,
                    transportReceiveConfiguration.InstanceSpecificQueue,
                    transportReceiveConfiguration.LogicalAddress,
                    transportReceiveConfiguration.PurgeOnStartup,
                    transportReceiveConfiguration.QueueNameBase,
                    TransactionMode = transportReceiveConfiguration.TransactionMode.ToString("G"),
                    transportReceiveConfiguration.PushRuntimeSettings.MaxConcurrency,
                    Satellites = transportReceiveConfiguration.SatelliteDefinitions.Select(s => new
                    {
                        s.Name,
                        s.ReceiveAddress,
                        TransactionMode = s.RequiredTransportTransactionMode.ToString("G"),
                        s.RuntimeSettings.MaxConcurrency
                    }).ToArray()
                });
            }

            return receiveComponent;
        }

        public async Task PrepareToStart(IBuilder builder, RecoverabilityComponent recoverabilityComponent, MessageOperations messageOperations)
        {
            if (IsSendOnly)
            {
                return;
            }

            mainPipelineExecutor = new MainPipelineExecutor(builder, pipeline, messageOperations);

            if (transportReceiveConfiguration.PurgeOnStartup)
            {
                Logger.Warn("All queues owned by the endpoint will be purged on startup.");
            }

            AddReceivers(builder, recoverabilityComponent.GetRecoverabilityExecutorFactory(builder));

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
        }

        public Task Stop()
        {
            var receiverStopTasks = receivers.Select(async receiver =>
            {
                Logger.DebugFormat("Stopping {0} receiver", receiver.Id);
                await receiver.Stop().ConfigureAwait(false);
                Logger.DebugFormat("Stopped {0} receiver", receiver.Id);
            });

            return Task.WhenAll(receiverStopTasks);
        }

        bool IsSendOnly => transportReceiveConfiguration == null;

        void BindQueues(QueueBindings queueBindings)
        {
            if (IsSendOnly)
            {
                return;
            }

            queueBindings.BindReceiving(transportReceiveConfiguration.LocalAddress);

            if (transportReceiveConfiguration.InstanceSpecificQueue != null)
            {
                queueBindings.BindReceiving(transportReceiveConfiguration.InstanceSpecificQueue);
            }

            foreach (var satellitePipeline in transportReceiveConfiguration.SatelliteDefinitions)
            {
                queueBindings.BindReceiving(satellitePipeline.ReceiveAddress);
            }
        }

        void AddReceivers(IBuilder builder, RecoverabilityExecutorFactory recoverabilityExecutorFactory)
        {
            var requiredTransactionSupport = transportReceiveConfiguration.TransactionMode;

            var recoverabilityExecutor = recoverabilityExecutorFactory.CreateDefault(eventAggregator, transportReceiveConfiguration.LocalAddress);
            var pushSettings = new PushSettings(transportReceiveConfiguration.LocalAddress, errorQueue, transportReceiveConfiguration.PurgeOnStartup, requiredTransactionSupport);
            var dequeueLimitations = transportReceiveConfiguration.PushRuntimeSettings;

            receivers.Add(new TransportReceiver(MainReceiverId, BuildMessagePump(), pushSettings, dequeueLimitations, mainPipelineExecutor, recoverabilityExecutor, criticalError));

            if (transportReceiveConfiguration.InstanceSpecificQueue != null)
            {
                var instanceSpecificQueue = transportReceiveConfiguration.InstanceSpecificQueue;
                var instanceSpecificRecoverabilityExecutor = recoverabilityExecutorFactory.CreateDefault(eventAggregator, instanceSpecificQueue);
                var sharedReceiverPushSettings = new PushSettings(instanceSpecificQueue, errorQueue, transportReceiveConfiguration.PurgeOnStartup, requiredTransactionSupport);

                receivers.Add(new TransportReceiver(MainReceiverId, BuildMessagePump(), sharedReceiverPushSettings, dequeueLimitations, mainPipelineExecutor, instanceSpecificRecoverabilityExecutor, criticalError));
            }

            foreach (var satellitePipeline in transportReceiveConfiguration.SatelliteDefinitions)
            {
                var satelliteRecoverabilityExecutor = recoverabilityExecutorFactory.Create(satellitePipeline.RecoverabilityPolicy, eventAggregator, satellitePipeline.ReceiveAddress);
                var satellitePushSettings = new PushSettings(satellitePipeline.ReceiveAddress, errorQueue, transportReceiveConfiguration.PurgeOnStartup, satellitePipeline.RequiredTransportTransactionMode);

                receivers.Add(new TransportReceiver(satellitePipeline.Name, BuildMessagePump(), satellitePushSettings, satellitePipeline.RuntimeSettings, new SatellitePipelineExecutor(builder, satellitePipeline), satelliteRecoverabilityExecutor, criticalError));
            }
        }

        IPushMessages BuildMessagePump()
        {
            return messagePumpFactory();
        }

        static void LoadMessageHandlers(Configuration configuration, List<Type> orderedTypes, IConfigureComponents container, ICollection<Type> availableTypes)
        {
            var types = new List<Type>(availableTypes);

            foreach (var t in orderedTypes)
            {
                types.Remove(t);
            }

            types.InsertRange(0, orderedTypes);

            ConfigureMessageHandlersIn(configuration, types, container);
        }

        static void ConfigureMessageHandlersIn(Configuration configuration, IEnumerable<Type> types, IConfigureComponents container)
        {
            var handlerRegistry = configuration.MessageHandlerRegistry ?? new MessageHandlerRegistry();

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

        public class Configuration
        {
            public Configuration(SettingsHolder settings)
            {
                this.settings = settings;
            }

            public List<Type> ExecuteTheseHandlersFirst => settings.GetOrCreate<List<Type>>();

            public MessageHandlerRegistry MessageHandlerRegistry => settings.GetOrDefault<MessageHandlerRegistry>();

            readonly SettingsHolder settings;

            const string ExecuteTheseHandlersFirstSettingKey = "NServiceBus.ExecuteTheseHandlersFirst";
        }

        ReceiveConfiguration transportReceiveConfiguration;
        List<TransportReceiver> receivers = new List<TransportReceiver>();
        Func<IPushMessages> messagePumpFactory;
        PipelineComponent pipeline;
        IPipelineExecutor mainPipelineExecutor;
        readonly IEventAggregator eventAggregator;
        CriticalError criticalError;
        string errorQueue;

        const string MainReceiverId = "Main";

        static Type IHandleMessagesType = typeof(IHandleMessages<>);
        static ILog Logger = LogManager.GetLogger<ReceiveComponent>();
    }
}