namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Logging;
    using Microsoft.Extensions.DependencyInjection;
    using Outbox;
    using Persistence;
    using Pipeline;
    using Transport;
    using Unicast;

    partial class ReceiveComponent
    {
        ReceiveComponent(TransportInfrastructure transportInfrastructure, 
            Configuration configuration,
            string errorQueue,
            TransportReceiver mainReceiver,
            TransportReceiver instanceReceiver)
        {
            TransportInfrastructure = transportInfrastructure;
            this.configuration = configuration;
            this.errorQueue = errorQueue;
            this.mainReceiver = mainReceiver;
            this.instanceReceiver = instanceReceiver;
        }

        public static async Task<ReceiveComponent> Initialize(TransportSeam.Settings settings, Configuration configuration,
            string errorQueue,
            HostingComponent.Configuration hostingConfiguration,
            PipelineSettings pipelineSettings)
        {

            var transportInfrastructure = await configuration.transportSeam.TransportDefinition.Initialize(
                    new Transport.Settings(hostingConfiguration.EndpointName,
                        hostingConfiguration.HostInformation.DisplayName, hostingConfiguration.StartupDiagnostics,
                        hostingConfiguration.CriticalError.Raise, hostingConfiguration.ShouldRunInstallers)
                )
                .ConfigureAwait(false);

            //RegisterTransportInfrastructureForBackwardsCompatibility
            settings.settings.Set(transportInfrastructure);

            if (configuration.IsSendOnlyEndpoint)
            {
                pipelineSettings.Register(new SendOnlySubscribeTerminator(), "Throws an exception when trying to subscribe from a send-only endpoint");
                pipelineSettings.Register(new SendOnlyUnsubscribeTerminator(), "Throws an exception when trying to unsubscribe from a send-only endpoint");
                return new ReceiveComponent(
                    transportInfrastructure,
                    configuration,
                    errorQueue,
                    null,
                    null);
            }

            var receivers = await AddReceivers(transportInfrastructure, configuration, errorQueue).ConfigureAwait(false);
            var receiveComponent = new ReceiveComponent(
                transportInfrastructure,
                configuration,
                errorQueue,
                receivers.mainReceiver,
                receivers.instanceReceiver);

            pipelineSettings.Register(new NativeSubscribeTerminator(receivers.mainReceiver.receiver.Subscriptions), "Requests the transport to subscribe to a given message type");
            pipelineSettings.Register(new NativeUnsubscribeTerminator(receivers.mainReceiver.receiver.Subscriptions), "Requests the transport to unsubscribe to a given message type");

            receiveComponent.BindQueues(configuration.transportSeam.QueueBindings);

            pipelineSettings.Register("TransportReceiveToPhysicalMessageProcessingConnector", b =>
            {
                var storage = b.GetService<IOutboxStorage>() ?? new NoOpOutboxStorage();
                return new TransportReceiveToPhysicalMessageConnector(storage);
            }, "Allows to abort processing the message");

            pipelineSettings.Register("LoadHandlersConnector", b =>
            {
                var adapter = b.GetService<ISynchronizedStorageAdapter>() ?? new NoOpSynchronizedStorageAdapter();
                var syncStorage = b.GetService<ISynchronizedStorage>() ?? new NoOpSynchronizedStorage();

                return new LoadHandlersConnector(b.GetRequiredService<MessageHandlerRegistry>(), syncStorage, adapter);
            }, "Gets all the handlers to invoke from the MessageHandler registry based on the message type.");

            pipelineSettings.Register("ExecuteUnitOfWork", new UnitOfWorkBehavior(), "Executes the UoW");

            pipelineSettings.Register("InvokeHandlers", new InvokeHandlerTerminator(), "Calls the IHandleMessages<T>.Handle(T)");

            var externalHandlerRegistryUsed = hostingConfiguration.Services.HasComponent<MessageHandlerRegistry>();
            var handlerDiagnostics = new Dictionary<string, List<string>>();

            if (!externalHandlerRegistryUsed)
            {
                var messageHandlerRegistry = configuration.messageHandlerRegistry;

                RegisterMessageHandlers(messageHandlerRegistry, configuration.ExecuteTheseHandlersFirst, hostingConfiguration.Services, hostingConfiguration.AvailableTypes);

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

        static TransportTransactionMode GetRequiredTransactionMode(Settings settings, TransportTransactionMode maxSupportedTransactionMode)
        {
            var transportTransactionSupport = maxSupportedTransactionMode;

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

        


        public async Task Start(IServiceProvider builder,
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
            var mainPipelineExecutor = new MainPipelineExecutor(builder, pipelineCache, messageOperations, configuration.PipelineCompletedSubscribers, receivePipeline);
            var recoverabilityExecutorFactory = recoverabilityComponent.GetRecoverabilityExecutorFactory(builder);
            var recoverability = recoverabilityExecutorFactory
                .CreateDefault(configuration.LocalAddress);

            await this.mainReceiver.Start(mainPipelineExecutor.Invoke, recoverability.Invoke).ConfigureAwait(false);
            if (instanceReceiver != null)
            {
                var instanceSpecificRecoverabilityExecutor = recoverabilityExecutorFactory.CreateDefault(configuration.InstanceSpecificQueue);
                await this.instanceReceiver.Start(mainPipelineExecutor.Invoke, instanceSpecificRecoverabilityExecutor.Invoke).ConfigureAwait(false);
            }
            
            foreach (var satellite in configuration.SatelliteDefinitions)
            {
                try
                {
                    //TODO use Wrap Satellite in a TransportReceiver too (or eliminate TransportReceiver completely)
                    satellite.Start(builder, recoverabilityExecutorFactory);
                }
                catch (Exception ex)
                {
                    Logger.Fatal($"Satellite failed to start.", ex);
                    throw;
                }
            }
        }

        public Task Stop()
        {
            var stopTasks = configuration.SatelliteDefinitions.Select(async satellite =>
            {
                await satellite.Stop().ConfigureAwait(false);
            }).ToList();
            stopTasks.Add(mainReceiver.Stop());
            if (instanceReceiver != null)
            {
                stopTasks.Add(instanceReceiver.Stop());
            }

            return Task.WhenAll(stopTasks);
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

        static async Task<(TransportReceiver mainReceiver, TransportReceiver instanceReceiver)> AddReceivers(TransportInfrastructure transportInfrastructure, Configuration configuration, string errorQueue)
        {
            var requiredTransactionSupport = configuration.TransactionMode;

            var pushSettings = new PushSettings(configuration.LocalAddress, errorQueue, configuration.PurgeOnStartup, requiredTransactionSupport);
            var dequeueLimitations = configuration.PushRuntimeSettings;

            var mainPump = await transportInfrastructure.CreateReceiver(new ReceiveSettings
            {
                ErrorQueueAddress = errorQueue,
                LocalAddress = configuration.LocalAddress,
                settings = pushSettings,
                UsePublishSubscribe = true
            }).ConfigureAwait(false);
            var mainReceiver = new TransportReceiver(MainReceiverId, mainPump, pushSettings, dequeueLimitations);

            TransportReceiver instanceReceiver = null;
            if (configuration.InstanceSpecificQueue != null)
            {
                var instanceSpecificQueue = configuration.InstanceSpecificQueue;
                var sharedReceiverPushSettings = new PushSettings(instanceSpecificQueue, errorQueue, configuration.PurgeOnStartup, requiredTransactionSupport);

                var instanceSpecificPump = await transportInfrastructure.CreateReceiver(new ReceiveSettings
                {
                    ErrorQueueAddress = errorQueue,
                    LocalAddress = instanceSpecificQueue,
                    settings = sharedReceiverPushSettings,
                    UsePublishSubscribe = false
                }).ConfigureAwait(false);
                instanceReceiver = new TransportReceiver(MainReceiverId, instanceSpecificPump, sharedReceiverPushSettings, dequeueLimitations);
            }

            foreach (var satelliteDefinition in configuration.SatelliteDefinitions)
            {
                await satelliteDefinition.Setup(transportInfrastructure, errorQueue,
                    configuration.PurgeOnStartup).ConfigureAwait(false);
            }

            return (mainReceiver, instanceReceiver);
        }

        static void RegisterMessageHandlers(MessageHandlerRegistry handlerRegistry, List<Type> orderedTypes, IServiceCollection container, ICollection<Type> availableTypes)
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

            container.AddSingleton(handlerRegistry);
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
        string errorQueue;

        const string MainReceiverId = "Main";

        static Type IHandleMessagesType = typeof(IHandleMessages<>);
        static ILog Logger = LogManager.GetLogger<ReceiveComponent>();
        private TransportReceiver mainReceiver;
        private TransportReceiver instanceReceiver;


        public TransportInfrastructure TransportInfrastructure { get; private set; }
    }
}