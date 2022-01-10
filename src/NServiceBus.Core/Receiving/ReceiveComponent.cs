namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
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
                return new ReceiveComponent(configuration);
            }

            var receiveComponent = new ReceiveComponent(configuration);

            hostingConfiguration.Services.AddSingleton(sp =>
            {
                var transport = configuration.transportSeam.GetTransportInfrastructure(sp);

                var mainReceiveAddress = transport.Receivers[MainReceiverId].ReceiveAddress;

                string instanceReceiveAddress = null;
                if (transport.Receivers.TryGetValue(InstanceSpecificReceiverId, out var instanceReceiver))
                {
                    instanceReceiveAddress = instanceReceiver.ReceiveAddress;
                }

                var satelliteReceiveAddresses = transport.Receivers.Values
                    .Where(r => r.Id != MainReceiverId && r.Id != InstanceSpecificReceiverId)
                    .Select(r => r.ReceiveAddress)
                    .ToArray();

                return new ReceiveAddresses(mainReceiveAddress, instanceReceiveAddress, satelliteReceiveAddresses);
            });

            hostingConfiguration.Services.AddSingleton(sp =>
            {
                var transport = configuration.transportSeam.GetTransportInfrastructure(sp);
                return transport.Receivers[MainReceiverId].Subscriptions;
            });

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

            var handlerDiagnostics = new Dictionary<string, List<string>>();


            var messageHandlerRegistry = configuration.messageHandlerRegistry;
            RegisterMessageHandlers(messageHandlerRegistry, configuration.ExecuteTheseHandlersFirst, hostingConfiguration.Services, hostingConfiguration.AvailableTypes);

            foreach (var messageType in messageHandlerRegistry.GetMessageTypes())
            {
                handlerDiagnostics[messageType.FullName] = messageHandlerRegistry.GetHandlersFor(messageType)
                    .Select(handler => handler.HandlerType.FullName)
                    .ToList();
            }

            var receiveSettings = new List<ReceiveSettings>
            {
                new ReceiveSettings(
                    MainReceiverId,
                    configuration.LocalQueueAddress,
                    configuration.transportSeam.TransportDefinition.SupportsPublishSubscribe,
                    configuration.PurgeOnStartup,
                    errorQueue)
            };

            if (configuration.InstanceSpecificQueueAddress != null)
            {
                receiveSettings.Add(new ReceiveSettings(
                    InstanceSpecificReceiverId,
                    configuration.InstanceSpecificQueueAddress,
                    false,
                    configuration.PurgeOnStartup,
                    errorQueue));
            }

            receiveSettings.AddRange(configuration.SatelliteDefinitions.Select(definition => new ReceiveSettings(
                definition.Name,
                definition.ReceiveAddress,
                false,
                configuration.PurgeOnStartup,
                errorQueue)));


            configuration.transportSeam.Configure(receiveSettings.ToArray());

            hostingConfiguration.AddStartupDiagnosticsSection("Receiving", new
            {
                configuration.LocalQueueAddress,
                configuration.InstanceSpecificQueueAddress,
                configuration.PurgeOnStartup,
                TransactionMode = configuration.transportSeam.TransportDefinition.TransportTransactionMode.ToString("G"),
                configuration.PushRuntimeSettings.MaxConcurrency,
                Satellites = configuration.SatelliteDefinitions.Select(s => new
                {
                    s.Name,
                    s.ReceiveAddress,
                    s.RuntimeSettings.MaxConcurrency
                }).ToArray(),
                MessageHandlers = handlerDiagnostics
            });

            return receiveComponent;
        }

        public async Task Initialize(
            IServiceProvider builder,
            RecoverabilityComponent recoverabilityComponent,
            MessageOperations messageOperations,
            PipelineComponent pipelineComponent,
            IPipelineCache pipelineCache,
            TransportInfrastructure transportInfrastructure,
            ConsecutiveFailuresConfiguration consecutiveFailuresConfiguration,
            CancellationToken cancellationToken = default)
        {
            if (configuration.IsSendOnlyEndpoint)
            {
                return;
            }

            var mainPump = CreateReceiver(consecutiveFailuresConfiguration, transportInfrastructure.Receivers[MainReceiverId]);

            var receivePipeline = pipelineComponent.CreatePipeline<ITransportReceiveContext>(builder);
            var mainPipelineExecutor = new MainPipelineExecutor(builder, pipelineCache, messageOperations, configuration.PipelineCompletedSubscribers, receivePipeline);
            var recoverabilityExecutorFactory = recoverabilityComponent.GetRecoverabilityExecutorFactory(builder);

            var recoverability = recoverabilityExecutorFactory
                .CreateDefault();

            await mainPump.Initialize(configuration.PushRuntimeSettings, mainPipelineExecutor.Invoke,
                recoverability.Invoke, cancellationToken).ConfigureAwait(false);
            receivers.Add(mainPump);

            if (transportInfrastructure.Receivers.TryGetValue(InstanceSpecificReceiverId, out var instanceSpecificPump))
            {
                var instancePump = CreateReceiver(consecutiveFailuresConfiguration, instanceSpecificPump);
                var instanceSpecificRecoverabilityExecutor = recoverabilityExecutorFactory.CreateDefault();

                await instancePump.Initialize(configuration.PushRuntimeSettings, mainPipelineExecutor.Invoke,
                    instanceSpecificRecoverabilityExecutor.Invoke, cancellationToken).ConfigureAwait(false);

                receivers.Add(instancePump);
            }

            foreach (var satellite in configuration.SatelliteDefinitions)
            {
                try
                {
                    var satellitePump = CreateReceiver(consecutiveFailuresConfiguration, transportInfrastructure.Receivers[satellite.Name]);
                    var satellitePipeline = new SatellitePipelineExecutor(builder, satellite);
                    var satelliteRecoverabilityExecutor = recoverabilityExecutorFactory.Create(satellite.RecoverabilityPolicy);

                    await satellitePump.Initialize(satellite.RuntimeSettings, satellitePipeline.Invoke,
                        satelliteRecoverabilityExecutor.Invoke, cancellationToken).ConfigureAwait(false);
                    receivers.Add(satellitePump);
                }
                catch (Exception ex) when (!ex.IsCausedBy(cancellationToken))
                {
                    Logger.Fatal("Satellite failed to start.", ex);
                    throw;
                }
            }
        }

        public async Task Start(CancellationToken cancellationToken = default)
        {
            foreach (var messageReceiver in receivers)
            {
                try
                {
                    Logger.DebugFormat("Receiver {0} is starting.", messageReceiver.Id);
                    await messageReceiver.StartReceive(cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex) when (!ex.IsCausedBy(cancellationToken))
                {
                    Logger.Fatal($"Receiver {messageReceiver.Id} failed to start.", ex);
                    throw;
                }
            }
        }

        public Task Stop(CancellationToken cancellationToken = default)
        {
            var receiverStopTasks = receivers.Select(async receiver =>
            {
                try
                {
                    Logger.DebugFormat("Stopping {0} receiver", receiver.Id);
                    await receiver.StopReceive(cancellationToken).ConfigureAwait(false);
                    Logger.DebugFormat("Stopped {0} receiver", receiver.Id);
                }
                catch (Exception ex) when (!ex.IsCausedBy(cancellationToken))
                {
                    Logger.Warn($"Receiver {receiver.Id} threw an exception on stopping.", ex);
                }
            });

            return Task.WhenAll(receiverStopTasks);
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

        IMessageReceiver CreateReceiver(ConsecutiveFailuresConfiguration consecutiveFailuresConfiguration, IMessageReceiver receiver)
        {
            if (consecutiveFailuresConfiguration.RateLimitSettings != null)
            {
                return new WrappedMessageReceiver(consecutiveFailuresConfiguration, receiver);
            }

            return receiver;
        }

        Configuration configuration;
        List<IMessageReceiver> receivers = new List<IMessageReceiver>();

        public const string MainReceiverId = "Main";
        public const string InstanceSpecificReceiverId = "InstanceSpecific";

        static Type IHandleMessagesType = typeof(IHandleMessages<>);
        static ILog Logger = LogManager.GetLogger<ReceiveComponent>();
    }
}