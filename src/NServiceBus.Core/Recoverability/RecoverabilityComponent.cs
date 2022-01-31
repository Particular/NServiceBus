﻿namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Hosting;
    using NServiceBus.Logging;
    using NServiceBus.Pipeline;
    using Settings;
    using Support;
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

        public RecoverabilityExecutorFactory GetRecoverabilityExecutorFactory()
        {
            if (recoverabilityExecutorFactory == null)
            {
                recoverabilityExecutorFactory = CreateRecoverabilityExecutorFactory();
            }

            return recoverabilityExecutorFactory;
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

            RecoverabilityConfig = new RecoverabilityConfig(immediateRetryConfig, delayedRetryConfig, failedConfig);

            pipelineSettings.Register(new RaiseRecoverabilityEventsBehavior(messageRetryNotification, messageFaultedNotification), "Emits the recoverability events.");
            pipelineSettings.Register(serviceProvider =>
            {
                var factory = CreateRecoverabilityExecutorFactory();

                var executor = factory.CreateRecoverabilityExecutor();

                return new RecoverabilityPipelineTerminator(executor);
            }, "Executes the configured retry policy");

            hostingConfiguration.AddStartupDiagnosticsSection("Recoverability", new
            {
                ImmediateRetries = RecoverabilityConfig.Immediate.MaxNumberOfRetries,
                DelayedRetries = RecoverabilityConfig.Delayed.MaxNumberOfRetries,
                DelayedRetriesTimeIncrease = RecoverabilityConfig.Delayed.TimeIncrease.ToString("g"),
                RecoverabilityConfig.Failed.ErrorQueue,
                UnrecoverableExceptions = RecoverabilityConfig.Failed.UnrecoverableExceptionTypes.Select(t => t.FullName).ToArray()
            });
        }

        public RecoverabilityPipelineExecutor CreateRecoverabilityPipelineExecutor(
            IServiceProvider serviceProvider,
            IPipelineCache pipelineCache,
            PipelineComponent pipeline,
            MessageOperations messageOperations)
        {
            var recoverabilityPipeline = pipeline.CreatePipeline<IRecoverabilityContext>(serviceProvider);

            if (!settings.TryGet(PolicyOverride, out Func<RecoverabilityConfig, ErrorContext, RecoverabilityAction> policy))
            {
                policy = DefaultRecoverabilityPolicy.Invoke;
            };

            return new RecoverabilityPipelineExecutor(
                serviceProvider,
                pipelineCache,
                messageOperations,
                RecoverabilityConfig,
                (errorContext) =>
                {
                    return AdjustForTransportCapabilities(policy(RecoverabilityConfig, errorContext));
                },
                recoverabilityPipeline);
        }

        public RecoverabilityExecutor CreateSatelliteRecoverabilityExecutor()
        {
            var factory = CreateRecoverabilityExecutorFactory();

            return factory.CreateRecoverabilityExecutor();
        }

        public RecoverabilityAction AdjustForTransportCapabilities(
            RecoverabilityAction selectedAction)
        {
            return AdjustForTransportCapabilities(
                RecoverabilityConfig.Failed.ErrorQueue,
                immediateRetriesAvailable,
                delayedRetriesAvailable,
                selectedAction);
        }

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

        RecoverabilityExecutorFactory CreateRecoverabilityExecutorFactory()
        {

            Func<MoveToErrorsExecutor> moveToErrorsExecutorFactory = () =>
            {
                var staticFaultMetadata = new Dictionary<string, string>
                {
                    {Headers.ProcessingMachine, RuntimeEnvironment.MachineName},
                    {Headers.ProcessingEndpoint, settings.EndpointName()},
                    {Headers.HostId, hostInformation.HostId.ToString("N")},
                    {Headers.HostDisplayName, hostInformation.DisplayName}
                };

                var headerCustomizations = settings.Get<Action<Dictionary<string, string>>>(FaultHeaderCustomization);

                return new MoveToErrorsExecutor(staticFaultMetadata, headerCustomizations);
            };

            Func<DelayedRetryExecutor> delayedRetryExecutorFactory = () =>
            {
                if (delayedRetriesAvailable)
                {
                    return new DelayedRetryExecutor();
                }

                return null;
            };

            return new RecoverabilityExecutorFactory(
                delayedRetryExecutorFactory,
                moveToErrorsExecutorFactory);
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

        public RecoverabilityConfig RecoverabilityConfig;
        Notification<MessageToBeRetried> messageRetryNotification;
        Notification<MessageFaulted> messageFaultedNotification;

        IReadOnlySettings settings;
        bool transactionsOn;
        bool delayedRetriesAvailable;
        bool immediateRetriesAvailable;
        RecoverabilityExecutorFactory recoverabilityExecutorFactory;
        HostInformation hostInformation;

        public const string NumberOfDelayedRetries = "Recoverability.Delayed.DefaultPolicy.Retries";
        public const string DelayedRetriesTimeIncrease = "Recoverability.Delayed.DefaultPolicy.Timespan";
        public const string NumberOfImmediateRetries = "Recoverability.Immediate.Retries";
        public const string FaultHeaderCustomization = "Recoverability.Failed.FaultHeaderCustomization";
        public const string PolicyOverride = "Recoverability.CustomPolicy";
        public const string UnrecoverableExceptions = "Recoverability.UnrecoverableExceptions";

        static int DefaultNumberOfRetries = 3;
        static TimeSpan DefaultTimeIncrease = TimeSpan.FromSeconds(10);
        static ILog Logger = LogManager.GetLogger<RecoverabilityComponent>();
        TransportSeam transportSeam;

        public class Configuration
        {
            public Notification<MessageToBeRetried> MessageRetryNotification { get; } = new Notification<MessageToBeRetried>();
            public Notification<MessageFaulted> MessageFaultedNotification { get; } = new Notification<MessageFaulted>();
        }
    }
}