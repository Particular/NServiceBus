﻿namespace NServiceBus;

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using NServiceBus.Hosting;
using NServiceBus.Logging;
using NServiceBus.Pipeline;
using NServiceBus.Support;
using Settings;
using Transport;

class RecoverabilityComponent
{
    public RecoverabilityComponent(SettingsHolder settings)
    {
        this.settings = settings;
        var configuration = settings.Get<Configuration>();
        messageRetryNotification = configuration.MessageRetryNotification;
        messageFaultedNotification = configuration.MessageFaultedNotification;
        settings.SetDefault(NumberOfDelayedRetries, DefaultNumberOfRetries);
        settings.SetDefault(DelayedRetriesTimeIncrease, DefaultTimeIncrease);
        settings.SetDefault(NumberOfImmediateRetries, 5);
        settings.SetDefault(FaultHeaderCustomization, new Action<Dictionary<string, string>>(headers => { }));
        settings.AddUnrecoverableException(typeof(MessageDeserializationException));
    }

    public void Initialize(
        ReceiveComponent.Configuration receiveConfiguration,
        HostingComponent.Configuration hostingConfiguration,
        TransportSeam transportSeam,
        PipelineSettings pipelineSettings)
    {
        if (receiveConfiguration.IsSendOnlyEndpoint)
        {
            //Message recoverability is only relevant for endpoints receiving messages.
            return;
        }

        hostInformation = hostingConfiguration.HostInformation;
        this.transportSeam = transportSeam;
        transactionsOn = transportSeam.TransportDefinition.TransportTransactionMode != TransportTransactionMode.None;
        delayedRetriesAvailable = transactionsOn && transportSeam.TransportDefinition.SupportsDelayedDelivery;
        immediateRetriesAvailable = transactionsOn;

        var errorQueue = settings.ErrorQueueAddress();
        transportSeam.QueueBindings.BindSending(errorQueue);

        var immediateRetryConfig = GetImmediateRetryConfig();

        var delayedRetryConfig = GetDelayedRetryConfig();

        var failedConfig = new FailedConfig(errorQueue, settings.UnrecoverableExceptions());

        recoverabilityConfig = new RecoverabilityConfig(immediateRetryConfig, delayedRetryConfig, failedConfig);

        faultMetadataExtractor = CreateFaultMetadataExtractor();

        pipelineSettings.Register(sp => new RecoverabilityRoutingConnector(sp.GetRequiredService<IncomingPipelineMetrics>(), messageRetryNotification, messageFaultedNotification), "Executes the configured retry policy");

        hostingConfiguration.AddStartupDiagnosticsSection("Recoverability", new
        {
            ImmediateRetries = recoverabilityConfig.Immediate.MaxNumberOfRetries,
            DelayedRetries = recoverabilityConfig.Delayed.MaxNumberOfRetries,
            DelayedRetriesTimeIncrease = recoverabilityConfig.Delayed.TimeIncrease.ToString("g"),
            recoverabilityConfig.Failed.ErrorQueue,
            UnrecoverableExceptions = recoverabilityConfig.Failed.UnrecoverableExceptionTypes.Select(t => t.FullName).ToArray()
        });
    }

    public IRecoverabilityPipelineExecutor CreateRecoverabilityPipelineExecutor(
        IServiceProvider serviceProvider,
        IPipelineCache pipelineCache,
        PipelineComponent pipeline,
        MessageOperations messageOperations)
    {
        var recoverabilityPipeline = pipeline.CreatePipeline<IRecoverabilityContext>(serviceProvider);

        if (!settings.TryGet(PolicyOverride, out Func<RecoverabilityConfig, ErrorContext, RecoverabilityAction> policy))
        {
            policy = (config, context) => DefaultRecoverabilityPolicy.Invoke(config, context);
        }

        return new RecoverabilityPipelineExecutor<(RecoverabilityComponent,
            Func<RecoverabilityConfig, ErrorContext, RecoverabilityAction>)>(
            serviceProvider,
            pipelineCache,
            messageOperations,
            recoverabilityConfig,
            (errorContext, state) =>
            {
                var (@this, localPolicy) = state;
                return AdjustForTransportCapabilities(
                    @this.recoverabilityConfig.Failed.ErrorQueue,
                    @this.immediateRetriesAvailable,
                    @this.delayedRetriesAvailable,
                    localPolicy(@this.recoverabilityConfig, errorContext));
            },
            recoverabilityPipeline,
            faultMetadataExtractor,
            (this, policy));
    }

    public IRecoverabilityPipelineExecutor CreateSatelliteRecoverabilityExecutor(
        IServiceProvider serviceProvider,
        Func<RecoverabilityConfig, ErrorContext, RecoverabilityAction> recoverabilityPolicy) =>
        new SatelliteRecoverabilityExecutor<(RecoverabilityComponent, Func<RecoverabilityConfig, ErrorContext, RecoverabilityAction>)>(
            serviceProvider,
            faultMetadataExtractor,
            (errorContext, state) =>
            {
                var (@this, policy) = state;
                return AdjustForTransportCapabilities(
                    @this.recoverabilityConfig.Failed.ErrorQueue,
                    @this.immediateRetriesAvailable,
                    @this.delayedRetriesAvailable,
                    policy(@this.recoverabilityConfig, errorContext));
            }, (this, recoverabilityPolicy));

    public static RecoverabilityAction AdjustForTransportCapabilities(
        string errorQueue,
        bool immediateRetriesAvailable,
        bool delayedRetriesAvailable,
        RecoverabilityAction selectedAction)
    {
        if (selectedAction is ImmediateRetry && !immediateRetriesAvailable)
        {
            Logger.Warn("Recoverability policy requested ImmediateRetry however immediate retries are not available with the current endpoint configuration. Moving message to error queue instead.");
            return RecoverabilityAction.MoveToError(errorQueue);
        }

        if (selectedAction is DelayedRetry && !delayedRetriesAvailable)
        {
            Logger.Warn("Recoverability policy requested DelayedRetry however delayed delivery capability is not available with the current endpoint configuration. Moving message to error queue instead.");
            return RecoverabilityAction.MoveToError(errorQueue);
        }

        return selectedAction;
    }

    FaultMetadataExtractor CreateFaultMetadataExtractor()
    {
        var staticFaultMetadata = new Dictionary<string, string>
            {
                {Headers.ProcessingMachine, RuntimeEnvironment.MachineName},
                {Headers.ProcessingEndpoint, settings.EndpointName()},
                {Headers.HostId, hostInformation.HostId.ToString("N")},
                {Headers.HostDisplayName, hostInformation.DisplayName}
            };

        var headerCustomizations = settings.Get<Action<Dictionary<string, string>>>(FaultHeaderCustomization);

        return new FaultMetadataExtractor(staticFaultMetadata, headerCustomizations);
    }

    ImmediateConfig GetImmediateRetryConfig()
    {
        var maxImmediateRetries = settings.Get<int>(NumberOfImmediateRetries);

        if (!transactionsOn && maxImmediateRetries > 0)
        {
            throw new Exception("Immediate retries are not supported when running with TransportTransactionMode.None.");
        }

        return new ImmediateConfig(maxImmediateRetries);
    }

    DelayedConfig GetDelayedRetryConfig()
    {
        var numberOfRetries = settings.Get<int>(NumberOfDelayedRetries);
        var timeIncrease = settings.Get<TimeSpan>(DelayedRetriesTimeIncrease);

        if (numberOfRetries > 0)
        {
            if (!transportSeam.TransportDefinition.SupportsDelayedDelivery)
            {
                throw new Exception("Delayed retries are not supported when the transport does not support delayed delivery. Disable delayed retries using 'endpointConfiguration.Recoverability().Delayed(settings => settings.NumberOfRetries(0))'.");
            }

            if (!transactionsOn)
            {
                throw new Exception("Delayed retries are not supported when running with TransportTransactionMode.None. Disable delayed retries using 'endpointConfiguration.Recoverability().Delayed(settings => settings.NumberOfRetries(0))' or select a different TransportTransactionMode.");
            }
        }

        return new DelayedConfig(numberOfRetries, timeIncrease);
    }

    readonly Notification<MessageToBeRetried> messageRetryNotification;
    readonly Notification<MessageFaulted> messageFaultedNotification;
    RecoverabilityConfig recoverabilityConfig;
    FaultMetadataExtractor faultMetadataExtractor;
    HostInformation hostInformation;
    readonly SettingsHolder settings;
    bool transactionsOn;
    bool delayedRetriesAvailable;
    bool immediateRetriesAvailable;

    public const string NumberOfDelayedRetries = "Recoverability.Delayed.DefaultPolicy.Retries";
    public const string DelayedRetriesTimeIncrease = "Recoverability.Delayed.DefaultPolicy.Timespan";
    public const string NumberOfImmediateRetries = "Recoverability.Immediate.Retries";
    public const string FaultHeaderCustomization = "Recoverability.Failed.FaultHeaderCustomization";
    public const string PolicyOverride = "Recoverability.CustomPolicy";
    public const string UnrecoverableExceptions = "Recoverability.UnrecoverableExceptions";

    static readonly int DefaultNumberOfRetries = 3;
    static readonly TimeSpan DefaultTimeIncrease = TimeSpan.FromSeconds(10);
    static readonly ILog Logger = LogManager.GetLogger<RecoverabilityComponent>();
    TransportSeam transportSeam;

    public class Configuration
    {
        public Notification<MessageToBeRetried> MessageRetryNotification { get; } = new Notification<MessageToBeRetried>();
        public Notification<MessageFaulted> MessageFaultedNotification { get; } = new Notification<MessageFaulted>();
    }
}