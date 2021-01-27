namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Faults;
    using Hosting;
    using Microsoft.Extensions.DependencyInjection;
    using Settings;
    using Support;
    using Transport;

    class RecoverabilityComponent
    {
        public RecoverabilityComponent(SettingsHolder settings)
        {
            this.settings = settings;
            var configuration = settings.Get<Configuration>();
            MessageRetryNotification = configuration.MessageRetryNotification;
            MessageFaultedNotification = configuration.MessageFaultedNotification;
            settings.SetDefault(NumberOfDelayedRetries, DefaultNumberOfRetries);
            settings.SetDefault(DelayedRetriesTimeIncrease, DefaultTimeIncrease);
            settings.SetDefault(NumberOfImmediateRetries, 5);
            settings.SetDefault(FaultHeaderCustomization, new Action<Dictionary<string, string>>(headers => { }));
            settings.AddUnrecoverableException(typeof(MessageDeserializationException));
        }

        public RecoverabilityExecutorFactory GetRecoverabilityExecutorFactory(IServiceProvider builder)
        {
            if (recoverabilityExecutorFactory == null)
            {
                recoverabilityExecutorFactory = CreateRecoverabilityExecutorFactory(builder);
            }

            return recoverabilityExecutorFactory;
        }

        public void Initialize(ReceiveComponent.Configuration receiveConfiguration, HostingComponent.Configuration hostingConfiguration, TransportSeam transportSeam)
        {
            if (receiveConfiguration.IsSendOnlyEndpoint)
            {
                //Message recoverability is only relevant for endpoints receiving messages.
                return;
            }

            hostInformation = hostingConfiguration.HostInformation;
            this.transportSeam = transportSeam;

            transactionsOn = transportSeam.TransportDefinition.TransportTransactionMode != TransportTransactionMode.None;

            var errorQueue = settings.ErrorQueueAddress();
            transportSeam.QueueBindings.BindSending(errorQueue);

            var immediateRetryConfig = GetImmediateRetryConfig();

            var delayedRetryConfig = GetDelayedRetryConfig();

            var failedConfig = new FailedConfig(errorQueue, settings.UnrecoverableExceptions());

            recoverabilityConfig = new RecoverabilityConfig(immediateRetryConfig, delayedRetryConfig, failedConfig);

            hostingConfiguration.AddStartupDiagnosticsSection("Recoverability", new
            {
                ImmediateRetries = recoverabilityConfig.Immediate.MaxNumberOfRetries,
                DelayedRetries = recoverabilityConfig.Delayed.MaxNumberOfRetries,
                DelayedRetriesTimeIncrease = recoverabilityConfig.Delayed.TimeIncrease.ToString("g"),
                recoverabilityConfig.Failed.ErrorQueue,
                UnrecoverableExceptions = recoverabilityConfig.Failed.UnrecoverableExceptionTypes.Select(t => t.FullName).ToArray()
            });
        }

        RecoverabilityExecutorFactory CreateRecoverabilityExecutorFactory(IServiceProvider builder)
        {
            var delayedRetriesAvailable = transactionsOn && transportSeam.TransportDefinition.SupportsDelayedDelivery;
            var immediateRetriesAvailable = transactionsOn;

            Func<string, MoveToErrorsExecutor> moveToErrorsExecutorFactory = localAddress =>
            {
                var staticFaultMetadata = new Dictionary<string, string>
                {
                    {FaultsHeaderKeys.FailedQ, localAddress},
                    {Headers.ProcessingMachine, RuntimeEnvironment.MachineName},
                    {Headers.ProcessingEndpoint, settings.EndpointName()},
                    {Headers.HostId, hostInformation.HostId.ToString("N")},
                    {Headers.HostDisplayName, hostInformation.DisplayName}
                };

                var headerCustomizations = settings.Get<Action<Dictionary<string, string>>>(FaultHeaderCustomization);

                return new MoveToErrorsExecutor(builder.GetRequiredService<IMessageDispatcher>(), staticFaultMetadata, headerCustomizations);
            };

            Func<string, DelayedRetryExecutor> delayedRetryExecutorFactory = localAddress =>
            {
                if (delayedRetriesAvailable)
                {
                    return new DelayedRetryExecutor(localAddress, builder.GetRequiredService<IMessageDispatcher>());
                }

                return null;
            };

            if (!settings.TryGet(PolicyOverride, out Func<RecoverabilityConfig, ErrorContext, RecoverabilityAction> policy))
            {
                policy = DefaultRecoverabilityPolicy.Invoke;
            }

            return new RecoverabilityExecutorFactory(
                policy,
                recoverabilityConfig,
                delayedRetryExecutorFactory,
                moveToErrorsExecutorFactory,
                immediateRetriesAvailable,
                delayedRetriesAvailable,
                MessageRetryNotification,
                MessageFaultedNotification);
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
                    throw new Exception("Delayed retries are not supported when the transport does not support delayed delivery.");
                }

                if (!transactionsOn)
                {
                    throw new Exception("Delayed retries are not supported when running with TransportTransactionMode.None.");
                }
            }

            return new DelayedConfig(numberOfRetries, timeIncrease);
        }

        public Notification<MessageToBeRetried> MessageRetryNotification;
        public Notification<MessageFaulted> MessageFaultedNotification;

        ReadOnlySettings settings;
        bool transactionsOn;
        RecoverabilityConfig recoverabilityConfig;
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
        TransportSeam transportSeam;

        public class Configuration
        {
            public Notification<MessageToBeRetried> MessageRetryNotification { get; } = new Notification<MessageToBeRetried>();
            public Notification<MessageFaulted> MessageFaultedNotification { get; } = new Notification<MessageFaulted>();
        }
    }
}