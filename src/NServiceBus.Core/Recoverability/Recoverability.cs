﻿namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using Config;
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
                settings.SetDefault(SlrNumberOfRetries, DefaultNumberOfRetries);
                settings.SetDefault(SlrTimeIncrease, DefaultTimeIncrease);
                settings.SetDefault(FlrNumberOfRetries, 5);
            });
        }

        protected internal override void Setup(FeatureConfigurationContext context)
        {
            var errorQueue = context.Settings.ErrorQueueAddress();
            context.Settings.Get<QueueBindings>().BindSending(errorQueue);


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

                    return new MoveToErrorsExecutor(b.Build<IDispatchMessages>(), errorQueue, staticFaultMetadata);
                };

                var transactionsOn = context.Settings.GetRequiredTransactionModeForReceives() != TransportTransactionMode.None;
                var delayedRetryConfig = GetDelayedRetryConfig(context.Settings, transactionsOn);
                Func<string, DelayedRetryExecutor> delayedRetryExecutorFactory = localAddress =>
                {
                    if (!transactionsOn) // TODO: Check timeout manager? Throw??
                    {
                        return null;
                    }
                    return new DelayedRetryExecutor(
                        localAddress,
                        b.Build<IDispatchMessages>(),
                        context.Settings.DoesTransportSupportConstraint<DelayedDeliveryConstraint>() ? null : context.Settings.Get<TimeoutManagerAddressConfiguration>().TransportAddress);
                };

                var immediateRetryConfig = GetImmediateRetryConfig(context.Settings, transactionsOn);

                Func<RecoverabilityConfig, ErrorContext, RecoverabilityAction> policy;
                if (!context.Settings.TryGet(PolicyOverride, out policy))
                {

                    policy = DefaultRecoverabilityPolicy.Invoke;
                }

                return new RecoverabilityExecutorFactory(
                    policy,
                    new RecoverabilityConfig(immediateRetryConfig, delayedRetryConfig),
                    delayedRetryExecutorFactory,
                    moveToErrorsExecutorFactory);
            }, DependencyLifecycle.SingleInstance);

            RaiseLegacyNotifications(context);
        }

        static ImmediateConfig GetImmediateRetryConfig(ReadOnlySettings settings, bool transactionsOn)
        {
            if (!transactionsOn)
            {
                Logger.Warn("Immediate Retries will be disabled. Immediate retries are not supported when running with TransportTransactionMode.None. Failed messages will be moved to the error queue instead.");
                //Transactions must be enabled since FLR requires the transport to be able to rollback
                return new ImmediateConfig(0);
            }

            var retriesConfig = settings.GetConfigSection<TransportConfig>();
            var maxImmediateRetries = retriesConfig?.MaxRetries ?? settings.Get<int>(FlrNumberOfRetries);

            return new ImmediateConfig(maxImmediateRetries);
        }

        static DelayedConfig GetDelayedRetryConfig(ReadOnlySettings settings, bool transactionsOn)
        {
            if (!transactionsOn)
            {
                Logger.Warn("Delayed Retries will be disabled. Delayed retries are not supported when running with TransportTransactionMode.None. Failed messages will be moved to the error queue instead.");
                //Transactions must be enabled since SLR requires the transport to be able to rollback
                return new DelayedConfig(0, TimeSpan.MinValue);
            }

            var numberOfRetries = settings.Get<int>(SlrNumberOfRetries);
            var timeIncrease = settings.Get<TimeSpan>(SlrTimeIncrease);

            var retriesConfig = settings.GetConfigSection<SecondLevelRetriesConfig>();
            if (retriesConfig != null)
            {
                numberOfRetries = retriesConfig.Enabled ? retriesConfig.NumberOfRetries : 0;
                timeIncrease = retriesConfig.TimeIncrease;
            }

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
                    legacyNotifications.Errors.InvokeMessageHasFailedAFirstLevelRetryAttempt(e.Attempt, e.Message, e.Exception);
                }
                else
                {
                    legacyNotifications.Errors.InvokeMessageHasBeenSentToSecondLevelRetries(e.Attempt, e.Message, e.Exception);
                }

                return TaskEx.CompletedTask;
            });

            notifications.Subscribe<MessageFaulted>(e =>
            {
                legacyNotifications.Errors.InvokeMessageHasBeenSentToErrorQueue(e.Message, e.Exception);
                return TaskEx.CompletedTask;
            });
        }

        public const string SlrNumberOfRetries = "Recoverability.Slr.DefaultPolicy.Retries";
        public const string SlrTimeIncrease = "Recoverability.Slr.DefaultPolicy.Timespan";
        public const string FlrNumberOfRetries = "Recoverability.Flr.Retries";
        public const string FaultHeaderCustomization = "Recoverability.Failed.FaultHeaderCustomization";
        public const string PolicyOverride = "Recoverability.CustomPolicy";

        static ILog Logger = LogManager.GetLogger<Recoverability>();
        internal static int DefaultNumberOfRetries = 3;
        internal static TimeSpan DefaultTimeIncrease = TimeSpan.FromSeconds(10);
    }
}