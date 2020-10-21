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
        ReceiveComponent(Configuration configuration)
        {
            this.configuration = configuration;
        }

        public static  ReceiveComponent Configure(
            TransportSeam.Settings settings, 
            Configuration configuration,
            string errorQueue,
            HostingComponent.Configuration hostingConfiguration,
            PipelineSettings pipelineSettings)
        {
            //RegisterTransportInfrastructureForBackwardsCompatibility
            settings.settings.Set(configuration.transportSeam);

            if (configuration.IsSendOnlyEndpoint)
            {
                configuration.transportSeam.Configure(new ReceiveSettings[0]);

                pipelineSettings.Register(new SendOnlySubscribeTerminator(), "Throws an exception when trying to subscribe from a send-only endpoint");
                pipelineSettings.Register(new SendOnlyUnsubscribeTerminator(), "Throws an exception when trying to unsubscribe from a send-only endpoint");

                return new ReceiveComponent(configuration);
            }

            var receivers = AddReceivers(configuration, errorQueue);

            configuration.transportSeam.Configure(receivers);

            var receiveComponent = new ReceiveComponent(configuration);

            //TODO needs better way to access receivers (via ID or some kind of reference), what about receiveSettings.GetReceiver(transportInfrastructure) that wraps the ID lookup?
            pipelineSettings.Register(new NativeSubscribeTerminator(() => receiveComponent.subscriptionManager), "Requests the transport to subscribe to a given message type");
            pipelineSettings.Register(new NativeUnsubscribeTerminator(() => receiveComponent.subscriptionManager), "Requests the transport to unsubscribe to a given message type");

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

        public void ConfigureSubscriptionManager(TransportInfrastructure transportInfrastructure)
        {
            this.subscriptionManager = transportInfrastructure.FindReceiver(MainReceiverId)?.Subscriptions;
        }

        public async Task Start(IServiceProvider builder,
            RecoverabilityComponent recoverabilityComponent,
            MessageOperations messageOperations,
            PipelineComponent pipelineComponent,
            IPipelineCache pipelineCache,
            TransportInfrastructure transportInfrastructure)
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


            var mainPump = transportInfrastructure.FindReceiver(MainReceiverId);
            this.mainReceiver = new TransportReceiver(mainPump, configuration.PushRuntimeSettings);
            await this.mainReceiver.Start(mainPipelineExecutor.Invoke, recoverability.Invoke).ConfigureAwait(false);

            var instanceSpecificPump = transportInfrastructure.FindReceiver(InstanceSpecificReceiverId);
            if (instanceSpecificPump != null)
            {
                var instanceSpecificRecoverabilityExecutor = recoverabilityExecutorFactory.CreateDefault(configuration.InstanceSpecificQueue);

                this.instanceReceiver = new TransportReceiver(instanceSpecificPump, configuration.PushRuntimeSettings);
                await this.instanceReceiver.Start(mainPipelineExecutor.Invoke, instanceSpecificRecoverabilityExecutor.Invoke).ConfigureAwait(false);
            }
            
            foreach (var satellite in configuration.SatelliteDefinitions)
            {
                try
                {
                    //TODO use Wrap Satellite in a TransportReceiver too (or eliminate TransportReceiver completely)
                    var satellitePump = transportInfrastructure.Receivers.First(r => r.Id == satellite.Name);
                    await satellite.Start(satellitePump, builder, recoverabilityExecutorFactory).ConfigureAwait(false);
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

        static ReceiveSettings[] AddReceivers(Configuration configuration, string errorQueue)
        {
            var requiredTransactionSupport = configuration.TransactionMode;

            var pushSettings = new PushSettings(configuration.LocalAddress, errorQueue, configuration.PurgeOnStartup, requiredTransactionSupport);

            var allReceivers = new List<ReceiveSettings>();

            allReceivers.Add(new ReceiveSettings
            {
                Id = MainReceiverId,
                ReceiveAddress = configuration.LocalAddress,
                settings = pushSettings,
                UsePublishSubscribe = true
            });

            //TransportReceiver instanceReceiver = null;
            if (configuration.InstanceSpecificQueue != null)
            {
                var instanceSpecificQueue = configuration.InstanceSpecificQueue;
                var sharedReceiverPushSettings = new PushSettings(instanceSpecificQueue, errorQueue, configuration.PurgeOnStartup, requiredTransactionSupport);

                allReceivers.Add(new ReceiveSettings
                {
                    Id = InstanceSpecificReceiverId,
                    ReceiveAddress = instanceSpecificQueue,
                    settings = sharedReceiverPushSettings,
                    UsePublishSubscribe = false
                });
            }

            foreach (var satelliteDefinition in configuration.SatelliteDefinitions)
            {
                var satelliteReceiverSettings = satelliteDefinition.Setup(errorQueue,
                    configuration.PurgeOnStartup);

                allReceivers.Add(satelliteReceiverSettings);
            }

            return allReceivers.ToArray();
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

        public const string MainReceiverId = "Main";
        public const string InstanceSpecificReceiverId = "Main-IS";

        static Type IHandleMessagesType = typeof(IHandleMessages<>);
        static ILog Logger = LogManager.GetLogger<ReceiveComponent>();
        private TransportReceiver mainReceiver;
        private TransportReceiver instanceReceiver;
        private IManageSubscriptions subscriptionManager;
    }
}