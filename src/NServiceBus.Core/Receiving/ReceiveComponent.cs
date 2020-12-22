using System.Collections.ObjectModel;
using System.Threading;
using NServiceBus.Transports;
using NServiceBus.Unicast.Messages;

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

        public static ReceiveComponent Configure(
            Configuration configuration,
            string errorQueue,
            HostingComponent.Configuration hostingConfiguration,
            PipelineSettings pipelineSettings)
        {
            if (configuration.IsSendOnlyEndpoint)
            {
                configuration.transportSeam.Configure(new ReceiveSettings[0]);

                pipelineSettings.Register(new SendOnlySubscribeTerminator(), "Throws an exception when trying to subscribe from a send-only endpoint");
                pipelineSettings.Register(new SendOnlyUnsubscribeTerminator(), "Throws an exception when trying to unsubscribe from a send-only endpoint");

                return new ReceiveComponent(configuration);
            }

            var receiveComponent = new ReceiveComponent(configuration);

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

        public void Initialize(TransportSeam transportSeam)
        {
            this.transportSeam = transportSeam;
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

            var mainPump = transportSeam.TransportInfrastructure.GetReceiver(MainReceiverId);

            var messageMetadataRegistry = builder.GetRequiredService<MessageMetadataRegistry>();
            var messageTypesHandled = GetEventTypesHandledByThisEndpoint(builder.GetRequiredService<MessageHandlerRegistry>(), builder.GetRequiredService<Conventions>());
            var mainReceiverEvents = messageTypesHandled.Select(t => messageMetadataRegistry.GetMessageMetadata(t)).ToList().AsReadOnly();

            await mainPump.Initialize(configuration.PushRuntimeSettings, mainPipelineExecutor.Invoke,
                recoverability.Invoke, mainReceiverEvents).ConfigureAwait(false);
            receivers.Add(mainPump);

            var instanceSpecificPump = transportSeam.TransportInfrastructure.GetReceiver(InstanceSpecificReceiverId);
            if (instanceSpecificPump != null)
            {
                var instanceSpecificRecoverabilityExecutor = recoverabilityExecutorFactory.CreateDefault(configuration.InstanceSpecificQueue);

                await instanceSpecificPump.Initialize(configuration.PushRuntimeSettings, mainPipelineExecutor.Invoke,
                    instanceSpecificRecoverabilityExecutor.Invoke, mainReceiverEvents).ConfigureAwait(false);

                receivers.Add(instanceSpecificPump);
            }

            foreach (var satellite in configuration.SatelliteDefinitions)
            {
                try
                {
                    var satellitePump = transportSeam.TransportInfrastructure.GetReceiver(satellite.Name);

                    var satellitePipeline = new SatellitePipelineExecutor(builder, satellite);
                    var satelliteRecoverabilityExecutor = recoverabilityExecutorFactory.Create(satellite.RecoverabilityPolicy, satellite.ReceiveAddress);

                    await satellitePump.Initialize(satellite.RuntimeSettings, satellitePipeline.Invoke,
                        satelliteRecoverabilityExecutor.Invoke, new MessageMetadata[0]).ConfigureAwait(false);
                    receivers.Add(satellitePump);
                }
                catch (Exception ex)
                {
                    Logger.Fatal("Satellite failed to start.", ex);
                    throw;
                }
            }

            foreach (var messageReceiver in receivers)
            {
                Logger.DebugFormat("Receiver {0} is starting.", messageReceiver.Id);

                await messageReceiver.StartReceive(CancellationToken.None).ConfigureAwait(false);
                //TODO: If we fails starting N-th receiver then we need to stop and dispose N-1 receivers
            }
        }

        static List<Type> GetEventTypesHandledByThisEndpoint(MessageHandlerRegistry handlerRegistry, Conventions conventions)
        {
            var messageTypesHandled = handlerRegistry.GetMessageTypes() //get all potential messages
                .Where(t => !conventions.IsInSystemConventionList(t)) //never auto-subscribe system messages
                .Where(t => !conventions.IsCommandType(t)) //commands should never be subscribed to
                .Where(t => conventions.IsEventType(t)) //only events unless the user asked for all messages
                                                        //TODO: respect SubscribeSettings.AutoSubscribeSagas setting
                                                        //.Where(t => settings.AutoSubscribeSagas || handlerRegistry.GetHandlersFor(t).Any(handler => !typeof(Saga).IsAssignableFrom(handler.HandlerType))) //get messages with other handlers than sagas if needed
                .ToList();

            //TODO respect SubscribeSettings.ExcludedTypes
            return messageTypesHandled;
        }

        public async Task Stop()
        {
            var receiverStopTasks = receivers.Select(async receiver =>
            {
                Logger.DebugFormat("Stopping {0} receiver", receiver.Id);
                try
                {
                    await receiver.StopReceive(CancellationToken.None).ConfigureAwait(false);
                    (receiver as IDisposable)?.Dispose();
                    Logger.DebugFormat("Stopped {0} receiver", receiver.Id);
                }
                catch (Exception exception)
                {
                    Logger.Warn($"Receiver {receiver.Id} threw an exception on stopping.", exception);
                }
            });

            await Task.WhenAll(receiverStopTasks).ConfigureAwait(false);
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
        List<IMessageReceiver> receivers = new List<IMessageReceiver>();

        public const string MainReceiverId = "Main";
        public const string InstanceSpecificReceiverId = "InstanceSpecific";

        static Type IHandleMessagesType = typeof(IHandleMessages<>);
        static ILog Logger = LogManager.GetLogger<ReceiveComponent>();
        TransportSeam transportSeam;
    }
}