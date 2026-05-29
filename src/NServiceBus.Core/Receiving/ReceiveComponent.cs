#nullable enable

namespace NServiceBus;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Logging;
using Microsoft.Extensions.DependencyInjection;
using MicrosoftLoggerFactory = Microsoft.Extensions.Logging.ILoggerFactory;
using Outbox;
using Pipeline;
using Transport;
using Unicast;

partial class ReceiveComponent
{
    ReceiveComponent(Configuration configuration, IActivityFactory activityFactory, EndpointLogSlot endpointLogSlot)
    {
        this.configuration = configuration;
        this.activityFactory = activityFactory;
        this.endpointLogSlot = endpointLogSlot;
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
            return new ReceiveComponent(configuration, hostingConfiguration.ActivityFactory, hostingConfiguration.EndpointLogSlot);
        }

        var receiveComponent = new ReceiveComponent(configuration, hostingConfiguration.ActivityFactory, hostingConfiguration.EndpointLogSlot);

        hostingConfiguration.Services.AddSingleton(sp =>
        {
            var transport = configuration.TransportSeam.GetTransportInfrastructure(sp);

            var mainReceiveAddress = transport.Receivers[MainReceiverId].ReceiveAddress;

            string? instanceReceiveAddress = null;
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

        pipelineSettings.Register("InvokeHandlers", sp => new InvokeHandlerTerminator(sp.GetRequiredService<IncomingPipelineMetrics>()), "Calls the IHandleMessages<T>.Handle(T)");

        var handlerDiagnostics = new Dictionary<string, List<string>>();

        var messageHandlerRegistry = configuration.MessageHandlerRegistry;

        hostingConfiguration.Services.AddSingleton(messageHandlerRegistry);
        foreach (var messageType in messageHandlerRegistry.GetMessageTypes())
        {
            handlerDiagnostics[messageType.FullName!] = [.. messageHandlerRegistry.GetHandlersFor(messageType).Select(handler => handler.HandlerType.FullName!)];
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
        EnvelopeComponent envelopeComponent,
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

        // It is fine to adapt multiple times since the deduplication happens inside the microsoft logger factory anyway.
        var slotFactory = LogManager.Adapt(builder.GetRequiredService<MicrosoftLoggerFactory>());

        // Resolving the logger here with the LogManager for now since we want to do a consistent sweep of all LogManager usage
        // in the next minor when GetLogger might be deprecated. But we do it late when the service provider is available
        // to make sure the logger is already properly wired up.
        var logger = LogManager.GetLogger<MessageHandlerRegistry>();
        if (logger.IsDebugEnabled)
        {
            var messageHandlerRegistry = configuration.MessageHandlerRegistry;
            foreach (var messageType in messageHandlerRegistry.GetMessageTypes())
            {
                var handlers = messageHandlerRegistry.GetHandlersFor(messageType);
                foreach (var messageHandler in handlers)
                {
                    logger.DebugFormat("Associated '{0}' message with '{1}' {2} handler.", messageType, messageHandler.HandlerType, messageHandler.IsTimeoutHandler ? "timeout" : "message");
                }
            }
        }

        var mainPump = CreateReceiver(consecutiveFailuresConfiguration, transportInfrastructure.Receivers[MainReceiverId], endpointLogSlot, slotFactory, manageSlotLifecycle: false);

        var receivePipeline = pipelineComponent.CreatePipeline<ITransportReceiveContext>(builder);

        var pipelineMetrics = builder.GetRequiredService<IncomingPipelineMetrics>();
        var envelopeUnwrapper = envelopeComponent.CreateUnwrapper(builder);
        var mainPipelineExecutor = new MainPipelineExecutor(builder, pipelineCache, messageOperations, configuration.PipelineCompletedSubscribers, receivePipeline, activityFactory, pipelineMetrics, envelopeUnwrapper);

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
            var instanceProcessingLogSlot = new EndpointReceiverLogSlot(endpointLogSlot, InstanceSpecificReceiverId);
            var instancePump = CreateReceiver(consecutiveFailuresConfiguration, instanceSpecificPump, instanceProcessingLogSlot, slotFactory, manageSlotLifecycle: true);
            var instancePipelineExecutor = new MainPipelineExecutor(builder, pipelineCache, messageOperations, configuration.PipelineCompletedSubscribers, receivePipeline, activityFactory, pipelineMetrics, envelopeUnwrapper);

            await instancePump.Initialize(
                configuration.PushRuntimeSettings,
                instancePipelineExecutor.Invoke,
                recoverabilityPipelineExecutor.Invoke,
                cancellationToken).ConfigureAwait(false);

            receivers.Add(instancePump);
        }

        foreach (var satellite in configuration.SatelliteDefinitions)
        {
            try
            {
                var satelliteLogSlot = new EndpointSatelliteLogSlot(endpointLogSlot, satellite.Name);
                var satellitePump = CreateReceiver(consecutiveFailuresConfiguration, transportInfrastructure.Receivers[satellite.Name], satelliteLogSlot, slotFactory, manageSlotLifecycle: true);

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

    static LogWrappedMessageReceiver CreateReceiver(ConsecutiveFailuresConfiguration consecutiveFailuresConfiguration, IMessageReceiver receiver, LogSlot logSlot, ILoggerFactory slotFactory, bool manageSlotLifecycle)
    {
        var effectiveReceiver = consecutiveFailuresConfiguration.RateLimitSettings is not null
            ? new WrappedMessageReceiver(consecutiveFailuresConfiguration, receiver)
            : receiver;

        return new LogWrappedMessageReceiver(effectiveReceiver, logSlot, slotFactory, manageSlotLifecycle);
    }

    readonly Configuration configuration;
    readonly IActivityFactory activityFactory;
    readonly EndpointLogSlot endpointLogSlot;
    readonly List<IMessageReceiver> receivers = [];

    const string MainReceiverId = "Main";
    const string InstanceSpecificReceiverId = "InstanceSpecific";

    static readonly ILog Logger = LogManager.GetLogger<ReceiveComponent>();
}