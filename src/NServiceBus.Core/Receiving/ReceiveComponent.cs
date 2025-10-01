namespace NServiceBus;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Logging;
using Microsoft.Extensions.DependencyInjection;
using Outbox;
using Pipeline;
using Transport;
using Unicast;

partial class ReceiveComponent
{
    ReceiveComponent(Configuration configuration, IActivityFactory activityFactory)
    {
        this.configuration = configuration;
        this.activityFactory = activityFactory;
    }

    public static ReceiveComponent Configure(
        Configuration configuration,
        string errorQueue,
        HostingComponent.Configuration hostingConfiguration,
        PipelineSettings pipelineSettings)
    {
        if (configuration.IsSendOnlyEndpoint)
        {
            configuration.TransportSeam.Configure([]);
            return new ReceiveComponent(configuration, hostingConfiguration.ActivityFactory);
        }

        var receiveComponent = new ReceiveComponent(configuration, hostingConfiguration.ActivityFactory);

        hostingConfiguration.Services.AddSingleton(sp =>
        {
            var transport = configuration.TransportSeam.GetTransportInfrastructure(sp);

            var mainReceiveAddress = transport.Receivers[MainReceiverId].ReceiveAddress;

            string instanceReceiveAddress = null;
            if (transport.Receivers.TryGetValue(InstanceSpecificReceiverId, out var instanceReceiver))
            {
                instanceReceiveAddress = instanceReceiver.ReceiveAddress;
            }

            var satelliteReceiveAddresses = transport.Receivers.Values
                .Where(r => r.Id is not MainReceiverId and not InstanceSpecificReceiverId)
                .Select(r => r.ReceiveAddress)
                .ToArray();

            return new ReceiveAddresses(mainReceiveAddress, instanceReceiveAddress, satelliteReceiveAddresses);
        });

        hostingConfiguration.Services.AddSingleton(sp =>
        {
            var transport = configuration.TransportSeam.GetTransportInfrastructure(sp);
            return transport.Receivers[MainReceiverId].Subscriptions;
        });

        pipelineSettings.Register("TransportReceiveToPhysicalMessageProcessingConnector", b =>
        {
            var storage = b.GetService<IOutboxStorage>() ?? new NoOpOutboxStorage();
            return new TransportReceiveToPhysicalMessageConnector(storage, b.GetRequiredService<IncomingPipelineMetrics>());
        }, "Allows to abort processing the message");

        pipelineSettings.Register("LoadHandlersConnector", b => new LoadHandlersConnector(b.GetRequiredService<MessageHandlerRegistry>(), hostingConfiguration.ActivityFactory), "Gets all the handlers to invoke from the MessageHandler registry based on the message type.");

        pipelineSettings.Register("InvokeHandlers", sp => new InvokeHandlerTerminator(sp.GetService<IncomingPipelineMetrics>()), "Calls the IHandleMessages<T>.Handle(T)");

        var handlerDiagnostics = new Dictionary<string, List<string>>();

        var messageHandlerRegistry = configuration.MessageHandlerRegistry;

        hostingConfiguration.Services.AddSingleton(messageHandlerRegistry);
        foreach (var messageType in messageHandlerRegistry.GetMessageTypes())
        {
            handlerDiagnostics[messageType.FullName] = messageHandlerRegistry.GetHandlersFor(messageType)
                .Select(handler => handler.HandlerType.FullName)
                .ToList();
        }

        var receiveSettings = new List<ReceiveSettings>
        {
            new(
                MainReceiverId,
                configuration.LocalQueueAddress,
                configuration.TransportSeam.TransportDefinition.SupportsPublishSubscribe,
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

        configuration.TransportSeam.Configure([.. receiveSettings]);

        hostingConfiguration.AddStartupDiagnosticsSection("Receiving", new
        {
            configuration.LocalQueueAddress,
            configuration.InstanceSpecificQueueAddress,
            configuration.PurgeOnStartup,
            TransactionMode = configuration.TransportSeam.TransportDefinition.TransportTransactionMode.ToString("G"),
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

        var pipelineMetrics = builder.GetService<IncomingPipelineMetrics>();
        var marshallers = builder.GetServices<IUnmarshalMessages>();
        var marshalingRouter = new UnmarshalingRouter(marshallers);
        var mainPipelineExecutor = new MainPipelineExecutor(builder, pipelineCache, messageOperations, configuration.PipelineCompletedSubscribers, receivePipeline, activityFactory, pipelineMetrics, marshalingRouter);

        var recoverabilityPipelineExecutor = recoverabilityComponent.CreateRecoverabilityPipelineExecutor(
            builder,
            pipelineCache,
            pipelineComponent,
            messageOperations);

        await mainPump.Initialize(
            configuration.PushRuntimeSettings,
            mainPipelineExecutor.Invoke,
            recoverabilityPipelineExecutor.Invoke,
            cancellationToken).ConfigureAwait(false);

        receivers.Add(mainPump);

        if (transportInfrastructure.Receivers.TryGetValue(InstanceSpecificReceiverId, out var instanceSpecificPump))
        {
            var instancePump = CreateReceiver(consecutiveFailuresConfiguration, instanceSpecificPump);

            await instancePump.Initialize(
                configuration.PushRuntimeSettings,
                mainPipelineExecutor.Invoke,
                recoverabilityPipelineExecutor.Invoke,
                cancellationToken).ConfigureAwait(false);

            receivers.Add(instancePump);
        }

        foreach (var satellite in configuration.SatelliteDefinitions)
        {
            try
            {
                var satellitePump = CreateReceiver(consecutiveFailuresConfiguration, transportInfrastructure.Receivers[satellite.Name]);
                var satellitePipeline = new SatellitePipelineExecutor(builder, satellite);
                var satelliteRecoverabilityExecutor = recoverabilityComponent.CreateSatelliteRecoverabilityExecutor(builder, satellite.RecoverabilityPolicy);

                await satellitePump.Initialize(
                        satellite.RuntimeSettings,
                        satellitePipeline.Invoke,
                        satelliteRecoverabilityExecutor.Invoke,
                        cancellationToken)
                    .ConfigureAwait(false);

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

    static IMessageReceiver CreateReceiver(ConsecutiveFailuresConfiguration consecutiveFailuresConfiguration, IMessageReceiver receiver)
    {
        if (consecutiveFailuresConfiguration.RateLimitSettings != null)
        {
            return new WrappedMessageReceiver(consecutiveFailuresConfiguration, receiver);
        }

        return receiver;
    }

    readonly Configuration configuration;
    readonly IActivityFactory activityFactory;
    readonly List<IMessageReceiver> receivers = [];

    const string MainReceiverId = "Main";
    const string InstanceSpecificReceiverId = "InstanceSpecific";

    static readonly ILog Logger = LogManager.GetLogger<ReceiveComponent>();
}