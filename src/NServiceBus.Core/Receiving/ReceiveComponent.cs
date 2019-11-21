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
    using Routing;
    using Settings;
    using Unicast;
    using Transport;

    class ReceiveComponent
    {
        ReceiveComponent(Configuration configuration,
            PipelineComponent pipelineComponent,
            CriticalError criticalError,
            string errorQueue,
            TransportReceiveInfrastructure transportReceiveInfrastructure)
        {
            this.configuration = configuration;
            this.pipelineComponent = pipelineComponent;
            this.criticalError = criticalError;
            this.errorQueue = errorQueue;
            this.transportReceiveInfrastructure = transportReceiveInfrastructure;
        }

        public static Configuration PrepareConfiguration(Settings settings, TransportSeam transportSeam)
        {
            var isSendOnlyEndpoint = settings.IsSendOnlyEndpoint;

            if (isSendOnlyEndpoint && settings.CustomLocalAddressProvided)
            {
                throw new Exception($"Specifying a base name for the input queue using `{nameof(ReceiveSettingsExtensions.OverrideLocalAddress)}(baseInputQueueName)` is not supported for send-only endpoints.");
            }

            var endpointName = settings.EndpointName;
            var discriminator = settings.EndpointInstanceDiscriminator;
            var queueNameBase = settings.CustomLocalAddress ?? endpointName;
            var purgeOnStartup = settings.PurgeOnStartup;

            var transportInfrastructure = transportSeam.TransportInfrastructure;

            //note: This is an old hack, we are passing the endpoint name to bind but we only care about the properties
            var mainInstanceProperties = transportInfrastructure.BindToLocalEndpoint(new EndpointInstance(endpointName)).Properties;

            var logicalAddress = LogicalAddress.CreateLocalAddress(queueNameBase, mainInstanceProperties);

            var localAddress = transportInfrastructure.ToTransportAddress(logicalAddress);

            string instanceSpecificQueue = null;
            if (discriminator != null)
            {
                instanceSpecificQueue = transportInfrastructure.ToTransportAddress(logicalAddress.CreateIndividualizedAddress(discriminator));
            }

            var transactionMode = GetRequiredTransactionMode(settings, transportInfrastructure);

            var pushRuntimeSettings = settings.PushRuntimeSettings;

            var receiveConfiguration = new Configuration(
                logicalAddress,
                queueNameBase,
                localAddress,
                instanceSpecificQueue,
                transactionMode,
                pushRuntimeSettings,
                purgeOnStartup,
                settings.PipelineCompletedSubscribers ?? new Notification<ReceivePipelineCompleted>(),
                isSendOnlyEndpoint,
                settings.ExecuteTheseHandlersFirst,
                settings.MessageHandlerRegistry,
                settings.ShouldCreateQueues,
                transportSeam);

            settings.RegisterReceiveConfigurationForBackwardsCompatibility(receiveConfiguration);

            return receiveConfiguration;
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

        public static ReceiveComponent Initialize(
            Configuration configuration,
            PipelineComponent pipelineComponent,
            string errorQueue,
            HostingComponent.Configuration hostingConfiguration,
            PipelineSettings pipelineSettings,
            InstallationComponent.Configuration installerConfiguration)
        {
            TransportReceiveInfrastructure transportReceiveInfrastructure = null;

            if (!configuration.IsSendOnlyEndpoint)
            {
                transportReceiveInfrastructure = configuration.transportSeam.TransportInfrastructure.ConfigureReceiveInfrastructure();

                if (configuration.CreateQueues)
                {
                    installerConfiguration.AddInstaller(identity =>
                    {
                        var queueCreator = transportReceiveInfrastructure.QueueCreatorFactory();
                        return queueCreator.CreateQueueIfNecessary(configuration.transportSeam.QueueBindings, identity);
                    });
                }
            }

            var receiveComponent = new ReceiveComponent(
                configuration,
                pipelineComponent,
                hostingConfiguration.CriticalError,
                errorQueue,
                transportReceiveInfrastructure);

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

            if (!hostingConfiguration.Container.HasComponent<MessageHandlerRegistry>())
            {
                var orderedHandlers = configuration.ExecuteTheseHandlersFirst;

                LoadMessageHandlers(configuration, orderedHandlers, hostingConfiguration.Container, hostingConfiguration.AvailableTypes);
            }

            if (!configuration.IsSendOnlyEndpoint)
            {
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
                    }).ToArray()
                });
            }

            return receiveComponent;
        }

        public async Task PrepareToStart(IBuilder builder,
            RecoverabilityComponent recoverabilityComponent,
            MessageOperations messageOperations,
        IPipelineCache pipelineCache)
        {
            if (configuration.IsSendOnlyEndpoint)
            {
                return;
            }

            var receivePipeline = pipelineComponent.CreatePipeline<ITransportReceiveContext>(builder);
            mainPipelineExecutor = new MainPipelineExecutor(builder, pipelineCache, messageOperations, configuration.PipelineCompletedSubscribers, receivePipeline);

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

            receivers.Add(new TransportReceiver(MainReceiverId, messagePumpFactory(), pushSettings, dequeueLimitations, mainPipelineExecutor, recoverabilityExecutor, criticalError));

            if (configuration.InstanceSpecificQueue != null)
            {
                var instanceSpecificQueue = configuration.InstanceSpecificQueue;
                var instanceSpecificRecoverabilityExecutor = recoverabilityExecutorFactory.CreateDefault(instanceSpecificQueue);
                var sharedReceiverPushSettings = new PushSettings(instanceSpecificQueue, errorQueue, configuration.PurgeOnStartup, requiredTransactionSupport);

                receivers.Add(new TransportReceiver(MainReceiverId, messagePumpFactory(), sharedReceiverPushSettings, dequeueLimitations, mainPipelineExecutor, instanceSpecificRecoverabilityExecutor, criticalError));
            }

            foreach (var satellitePipeline in configuration.SatelliteDefinitions)
            {
                var satelliteRecoverabilityExecutor = recoverabilityExecutorFactory.Create(satellitePipeline.RecoverabilityPolicy, satellitePipeline.ReceiveAddress);
                var satellitePushSettings = new PushSettings(satellitePipeline.ReceiveAddress, errorQueue, configuration.PurgeOnStartup, satellitePipeline.RequiredTransportTransactionMode);

                receivers.Add(new TransportReceiver(satellitePipeline.Name, messagePumpFactory(), satellitePushSettings, satellitePipeline.RuntimeSettings, new SatellitePipelineExecutor(builder, satellitePipeline), satelliteRecoverabilityExecutor, criticalError));
            }
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
            var handlerRegistry = configuration.messageHandlerRegistry;

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

        Configuration configuration;
        List<TransportReceiver> receivers = new List<TransportReceiver>();
        readonly PipelineComponent pipelineComponent;
        IPipelineExecutor mainPipelineExecutor;
        CriticalError criticalError;
        string errorQueue;
        readonly TransportReceiveInfrastructure transportReceiveInfrastructure;

        const string MainReceiverId = "Main";

        static Type IHandleMessagesType = typeof(IHandleMessages<>);
        static ILog Logger = LogManager.GetLogger<ReceiveComponent>();

        public class Settings
        {
            public Settings(SettingsHolder settings)
            {
                this.settings = settings;
            }

            public List<Type> ExecuteTheseHandlersFirst => settings.GetOrCreate<List<Type>>();

            public MessageHandlerRegistry MessageHandlerRegistry => settings.GetOrCreate<MessageHandlerRegistry>();

            public bool ShouldCreateQueues
            {
                get => settings.Get<bool>("Transport.CreateQueues");
                set => settings.Set("Transport.CreateQueues", value);
            }

            public bool CustomLocalAddressProvided => settings.HasExplicitValue(ReceiveSettingsExtensions.CustomLocalAddressKey);

            public string CustomLocalAddress => settings.GetOrDefault<string>(ReceiveSettingsExtensions.CustomLocalAddressKey);

            public string EndpointName => settings.EndpointName();

            public string EndpointInstanceDiscriminator => settings.GetOrDefault<string>(EndpointInstanceDiscriminatorSettingsKey);

            public bool UserHasProvidedTransportTransactionMode => settings.HasSetting<TransportTransactionMode>();

            public TransportTransactionMode UserTransportTransactionMode => settings.Get<TransportTransactionMode>();

            public bool PurgeOnStartup
            {
                get => settings.GetOrDefault<bool>(TransportPurgeOnStartupSettingsKey);
                set => settings.Set(TransportPurgeOnStartupSettingsKey, value);
            }

            public PushRuntimeSettings PushRuntimeSettings
            {
                get
                {
                    if (settings.TryGet(out PushRuntimeSettings value))
                    {
                        return value;
                    }

                    return PushRuntimeSettings.Default;
                }
                set => settings.Set(value);
            }

            public Notification<ReceivePipelineCompleted> PipelineCompletedSubscribers => settings.GetOrCreate<Notification<ReceivePipelineCompleted>>();

            public bool IsSendOnlyEndpoint => settings.Get<bool>(EndpointSendOnlySettingKey);

            readonly SettingsHolder settings;

            const string EndpointInstanceDiscriminatorSettingsKey = "EndpointInstanceDiscriminator";
            const string TransportPurgeOnStartupSettingsKey = "Transport.PurgeOnStartup";
            const string EndpointSendOnlySettingKey = "Endpoint.SendOnly";

            public void RegisterReceiveConfigurationForBackwardsCompatibility(Configuration configuration)
            {
                //note: remove once settings.LogicalAddress() , .LocalAddress() and .InstanceSpecificQueue() has been obsoleted
                settings.Set(configuration);
            }

            public void SetDefaultPushRuntimeSettings(PushRuntimeSettings pushRuntimeSettings)
            {
                settings.SetDefault(pushRuntimeSettings);
            }
        }

        public class Configuration
        {
            public Configuration(LogicalAddress logicalAddress,
                string queueNameBase,
                string localAddress,
                string instanceSpecificQueue,
                TransportTransactionMode transactionMode,
                PushRuntimeSettings pushRuntimeSettings,
                bool purgeOnStartup,
                Notification<ReceivePipelineCompleted> pipelineCompletedSubscribers,
                bool isSendOnlyEndpoint,
                List<Type> executeTheseHandlersFirst,
                MessageHandlerRegistry messageHandlerRegistry,
                bool createQueues, TransportSeam transportSeam)
            {
                LogicalAddress = logicalAddress;
                QueueNameBase = queueNameBase;
                LocalAddress = localAddress;
                InstanceSpecificQueue = instanceSpecificQueue;
                TransactionMode = transactionMode;
                PushRuntimeSettings = pushRuntimeSettings;
                PurgeOnStartup = purgeOnStartup;
                IsSendOnlyEndpoint = isSendOnlyEndpoint;
                PipelineCompletedSubscribers = pipelineCompletedSubscribers;
                ExecuteTheseHandlersFirst = executeTheseHandlersFirst;
                satelliteDefinitions = new List<SatelliteDefinition>();
                this.messageHandlerRegistry = messageHandlerRegistry;
                this.CreateQueues = createQueues;
                this.transportSeam = transportSeam;
            }

            public LogicalAddress LogicalAddress { get; }

            public string LocalAddress { get; }

            public string InstanceSpecificQueue { get; }

            public TransportTransactionMode TransactionMode { get; }

            public PushRuntimeSettings PushRuntimeSettings { get; }

            public string QueueNameBase { get; }

            public IReadOnlyList<SatelliteDefinition> SatelliteDefinitions => satelliteDefinitions;

            public bool PurgeOnStartup { get; }

            public bool IsSendOnlyEndpoint { get; }

            public Notification<ReceivePipelineCompleted> PipelineCompletedSubscribers;

            public List<Type> ExecuteTheseHandlersFirst { get; }

            public bool CreateQueues { get; }

            public void AddSatelliteReceiver(string name, string transportAddress, PushRuntimeSettings runtimeSettings, Func<RecoverabilityConfig, ErrorContext, RecoverabilityAction> recoverabilityPolicy, Func<IBuilder, MessageContext, Task> onMessage)
            {
                var satelliteDefinition = new SatelliteDefinition(name, transportAddress, TransactionMode, runtimeSettings, recoverabilityPolicy, onMessage);

                satelliteDefinitions.Add(satelliteDefinition);
            }

            List<SatelliteDefinition> satelliteDefinitions;

            //This should only be used by the receive component it self
            internal readonly MessageHandlerRegistry messageHandlerRegistry;
            internal readonly TransportSeam transportSeam;
        }
    }
}