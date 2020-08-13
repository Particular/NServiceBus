namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using DelayedDelivery;
    using DeliveryConstraints;
    using Faults;
    using Hosting;
    using Logging;
    using Microsoft.Extensions.DependencyInjection;
    using ObjectBuilder;
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

        public RecoverabilityExecutorFactory GetRecoverabilityExecutorFactory(IBuilder builder)
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

            transactionsOn = receiveConfiguration.TransactionMode != TransportTransactionMode.None;

            var errorQueue = settings.ErrorQueueAddress();
            transportSeam.QueueBindings.BindSending(errorQueue);

            var delayedRetryConfig = GetDelayedRetryConfig();

            var immediateRetryConfig = GetImmediateRetryConfig();

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

        RecoverabilityExecutorFactory CreateRecoverabilityExecutorFactory(IBuilder builder)
        {
            var delayedRetriesAvailable = transactionsOn
                                          && (settings.DoesTransportSupportConstraint<DelayedDeliveryConstraint>() || settings.Get<TimeoutManagerAddressConfiguration>().TransportAddress != null);

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

                return new MoveToErrorsExecutor(builder.GetService<IDispatchMessages>(), staticFaultMetadata, headerCustomizations);
            };

            Func<string, DelayedRetryExecutor> delayedRetryExecutorFactory = localAddress =>
            {
                if (delayedRetriesAvailable)
                {
                    return new DelayedRetryExecutor(
                        localAddress,
                        builder.GetService<IDispatchMessages>(),
                        settings.DoesTransportSupportConstraint<DelayedDeliveryConstraint>()
                            ? null
                            : settings.Get<TimeoutManagerAddressConfiguration>().TransportAddress);
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
            if (!transactionsOn)
            {
                Logger.Warn("Immediate Retries will be disabled. Immediate Retries are not supported when running with TransportTransactionMode.None. Failed messages will be moved to the error queue instead.");
                //Transactions must be enabled since Immediate Retries requires the transport to be able to rollback
                return new ImmediateConfig(0);
            }

            var maxImmediateRetries = settings.Get<int>(NumberOfImmediateRetries);

            return new ImmediateConfig(maxImmediateRetries);
        }

        DelayedConfig GetDelayedRetryConfig()
        {
            if (!transactionsOn)
            {
                Logger.Warn("Delayed Retries will be disabled. Delayed retries are not supported when running with TransportTransactionMode.None. Failed messages will be moved to the error queue instead.");
                //Transactions must be enabled since Delayed Retries requires the transport to be able to rollback
                return new DelayedConfig(0, TimeSpan.Zero);
            }

            var numberOfRetries = settings.Get<int>(NumberOfDelayedRetries);
            var timeIncrease = settings.Get<TimeSpan>(DelayedRetriesTimeIncrease);

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
        static ILog Logger = LogManager.GetLogger<RecoverabilityComponent>();

        public class Configuration
        {
            public Notification<MessageToBeRetried> MessageRetryNotification { get; } = new Notification<MessageToBeRetried>();
            public Notification<MessageFaulted> MessageFaultedNotification { get; } = new Notification<MessageFaulted>();
        }
    }
}