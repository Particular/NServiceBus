namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using ConsistencyGuarantees;
    using DelayedDelivery;
    using DeliveryConstraints;
    using Faults;
    using Features;
    using Hosting;
    using Logging;
    using Settings;
    using Support;
    using Transport;

    class Recoverability : Feature
    {
        public Recoverability()
        {
            EnableByDefault();
            DependsOnOptionally<DelayedDeliveryFeature>();

            Prerequisite(context => !context.Settings.GetOrDefault<bool>("Endpoint.SendOnly"),
                "Message recoverability is only relevant for endpoints receiving messages.");
            Defaults(settings =>
            {
                settings.SetDefault(NumberOfDelayedRetries, DefaultNumberOfRetries);
                settings.SetDefault(DelayedRetriesTimeIncrease, DefaultTimeIncrease);
                settings.SetDefault(NumberOfImmediateRetries, 5);
                settings.SetDefault(FaultHeaderCustomization, new Action<Dictionary<string, string>>(headers => { }));
                settings.AddUnrecoverableException(typeof(MessageDeserializationException));
            });
        }

        protected internal override void Setup(FeatureConfigurationContext context)
        {
            var errorQueue = context.Settings.ErrorQueueAddress();
            context.Settings.Get<QueueBindings>().BindSending(errorQueue);

            var transactionsOn = context.Settings.GetRequiredTransactionModeForReceives() != TransportTransactionMode.None;
            var delayedRetryConfig = GetDelayedRetryConfig(context.Settings, transactionsOn);
            var delayedRetriesAvailable = transactionsOn
                                          && (context.Settings.DoesTransportSupportConstraint<DelayedDeliveryConstraint>() || context.Settings.Get<TimeoutManagerAddressConfiguration>().TransportAddress != null);


            var immediateRetryConfig = GetImmediateRetryConfig(context.Settings, transactionsOn);
            var immediateRetriesAvailable = transactionsOn;

            var failedConfig = new FailedConfig(errorQueue, context.Settings.UnrecoverableExceptions());

            var recoverabilityConfig = new RecoverabilityConfig(immediateRetryConfig, delayedRetryConfig, failedConfig);

            context.Settings.AddStartupDiagnosticsSection("Recoverability", new
            {
                ImmediateRetries = recoverabilityConfig.Immediate.MaxNumberOfRetries,
                DelayedRetries = recoverabilityConfig.Delayed.MaxNumberOfRetries,
                DelayedRetriesTimeIncrease = recoverabilityConfig.Delayed.TimeIncrease.ToString("g"),
                recoverabilityConfig.Failed.ErrorQueue,
                UnrecoverableExceptions = recoverabilityConfig.Failed.UnrecoverableExceptionTypes.Select(t => t.FullName).ToArray()
            });

            context.Container.ConfigureComponent(b =>
            {
                Func<string, MoveToErrorsExecutor> moveToErrorsExecutorFactory = localAddress =>
                {
                    var hostInfo = b.Build<HostInformation>();
                    var staticFaultMetadata = new Dictionary<string, string>
                    {
                        {FaultsHeaderKeys.FailedQ, localAddress},
                        {Headers.ProcessingMachine, RuntimeEnvironment.MachineName},
                        {Headers.ProcessingEndpoint, context.Settings.EndpointName()},
                        {Headers.HostId, hostInfo.HostId.ToString("N")},
                        {Headers.HostDisplayName, hostInfo.DisplayName}
                    };

                    var headerCustomizations = context.Settings.Get<Action<Dictionary<string, string>>>(FaultHeaderCustomization);

                    return new MoveToErrorsExecutor(b.Build<IDispatchMessages>(), staticFaultMetadata, headerCustomizations);
                };

                Func<string, DelayedRetryExecutor> delayedRetryExecutorFactory = localAddress =>
                {
                    if (delayedRetriesAvailable)
                    {
                        return new DelayedRetryExecutor(
                            localAddress,
                            b.Build<IDispatchMessages>(),
                            context.Settings.DoesTransportSupportConstraint<DelayedDeliveryConstraint>()
                                ? null
                                : context.Settings.Get<TimeoutManagerAddressConfiguration>().TransportAddress);
                    }

                    return null;
                };

                if (!context.Settings.TryGet(PolicyOverride, out Func<RecoverabilityConfig, ErrorContext, RecoverabilityAction> policy))
                {
                    policy = DefaultRecoverabilityPolicy.Invoke;
                }

                return new RecoverabilityExecutorFactory(
                    policy,
                    recoverabilityConfig,
                    delayedRetryExecutorFactory,
                    moveToErrorsExecutorFactory,
                    immediateRetriesAvailable,
                    delayedRetriesAvailable);

            }, DependencyLifecycle.SingleInstance);

            RaiseLegacyNotifications(context);
        }

        static ImmediateConfig GetImmediateRetryConfig(ReadOnlySettings settings, bool transactionsOn)
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

        static DelayedConfig GetDelayedRetryConfig(ReadOnlySettings settings, bool transactionsOn)
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

        //note: will soon be removed since we're deprecating Notifications in favor of the new notifications
        static void RaiseLegacyNotifications(FeatureConfigurationContext context)
        {
            var legacyNotifications = context.Settings.Get<Notifications>();
            var notifications = context.Settings.Get<NotificationSubscriptions>();

            notifications.Subscribe<MessageToBeRetried>(e =>
            {
                if (e.IsImmediateRetry)
                {
                    legacyNotifications.Errors.InvokeMessageHasFailedAnImmediateRetryAttempt(e.Attempt, e.Message, e.Exception);
                }
                else
                {
                    legacyNotifications.Errors.InvokeMessageHasBeenSentToDelayedRetries(e.Attempt, e.Message, e.Exception);
                }

                return TaskEx.CompletedTask;
            });

            notifications.Subscribe<MessageFaulted>(e =>
            {
                legacyNotifications.Errors.InvokeMessageHasBeenSentToErrorQueue(e.Message, e.Exception, e.ErrorQueue);
                return TaskEx.CompletedTask;
            });
        }

        public const string NumberOfDelayedRetries = "Recoverability.Delayed.DefaultPolicy.Retries";
        public const string DelayedRetriesTimeIncrease = "Recoverability.Delayed.DefaultPolicy.Timespan";
        public const string NumberOfImmediateRetries = "Recoverability.Immediate.Retries";
        public const string FaultHeaderCustomization = "Recoverability.Failed.FaultHeaderCustomization";
        public const string PolicyOverride = "Recoverability.CustomPolicy";
        public const string UnrecoverableExceptions = "Recoverability.UnrecoverableExceptions";

        static ILog Logger = LogManager.GetLogger<Recoverability>();

        internal static int DefaultNumberOfRetries = 3;
        internal static TimeSpan DefaultTimeIncrease = TimeSpan.FromSeconds(10);
    }
}