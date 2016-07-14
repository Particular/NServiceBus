namespace NServiceBus
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
                settings.SetDefault(SlrNumberOfRetries, DefaultSecondLevelRetryPolicy.DefaultNumberOfRetries);
                settings.SetDefault(SlrTimeIncrease, DefaultSecondLevelRetryPolicy.DefaultTimeIncrease);

                settings.SetDefault(FlrNumberOfRetries, 5);
            });
        }

        protected internal override void Setup(FeatureConfigurationContext context)
        {
            var errorQueue = context.Settings.ErrorQueueAddress();
            context.Settings.Get<QueueBindings>().BindSending(errorQueue);

            var transportTransactionMode = context.Settings.GetRequiredTransactionModeForReceives();

            if (transportTransactionMode == TransportTransactionMode.None)
            {
                Logger.Warn("First and Second Level Retries will be disabled. Automatic retries are not supported when running with TransportTransactionMode.None. Failed messages will be moved to the error queue instead.");
            }

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

                var delayedRetriesEnabled = IsDelayedRetriesEnabled(context.Settings);
                Func<string, DelayedRetryExecutor> delayedRetryExecutorFactory = localAddress =>
                {
                    if (!delayedRetriesEnabled || transportTransactionMode == TransportTransactionMode.None)
                    {
                        return null;
                    }
                    return new DelayedRetryExecutor(
                        localAddress,
                        b.Build<IDispatchMessages>(),
                        context.DoesTransportSupportConstraint<DelayedDeliveryConstraint>() ? null : context.Settings.Get<TimeoutManagerAddressConfiguration>().TransportAddress);
                };

                var delayedRetryPolicy = delayedRetriesEnabled ? GetDelayedRetryPolicy(context.Settings) : null;

                var immediateRetriesEnabled = IsImmediateRetriesEnabled(context.Settings);
                var maxImmediateRetries = immediateRetriesEnabled ? GetMaxImmediateRetries(context.Settings) : 0;


                var defaultRecoverabilityPolicy = new DefaultRecoverabilityPolicy(immediateRetriesEnabled, delayedRetriesEnabled, maxImmediateRetries, delayedRetryPolicy);

                return new RecoverabilityExecutorFactory(
                    defaultRecoverabilityPolicy,
                    delayedRetryExecutorFactory,
                    moveToErrorsExecutorFactory,
                    transportTransactionMode);
            }, DependencyLifecycle.SingleInstance);

            RaiseLegacyNotifications(context);
        }

        static SecondLevelRetryPolicy GetDelayedRetryPolicy(ReadOnlySettings settings)
        {
            Func<SecondLevelRetryContext, TimeSpan> customRetryPolicy;
            if (settings.TryGet(SlrCustomPolicy, out customRetryPolicy))
            {
                return new CustomSecondLevelRetryPolicy(customRetryPolicy);
            }

            var numberOfRetries = settings.Get<int>(SlrNumberOfRetries);
            var timeIncrease = settings.Get<TimeSpan>(SlrTimeIncrease);

            var retriesConfig = settings.GetConfigSection<SecondLevelRetriesConfig>();
            if (retriesConfig != null)
            {
                numberOfRetries = retriesConfig.Enabled ? retriesConfig.NumberOfRetries : 0;
                timeIncrease = retriesConfig.TimeIncrease;
            }

            return new DefaultSecondLevelRetryPolicy(numberOfRetries, timeIncrease);
        }

        bool IsDelayedRetriesEnabled(ReadOnlySettings settings)
        {
            //Transactions must be enabled since SLR requires the transport to be able to rollback
            if (settings.GetRequiredTransactionModeForReceives() == TransportTransactionMode.None)
            {
                return false;
            }

            Func<SecondLevelRetryContext, TimeSpan> customPolicy;
            if (settings.TryGet(SlrCustomPolicy, out customPolicy))
            {
                return true;
            }

            var retriesConfig = settings.GetConfigSection<SecondLevelRetriesConfig>();
            if (retriesConfig != null && retriesConfig.Enabled && retriesConfig.NumberOfRetries > 0)
            {
                return true;
            }

            if (settings.Get<int>(SlrNumberOfRetries) > 0)
            {
                return true;
            }

            return false;
        }

        bool IsImmediateRetriesEnabled(ReadOnlySettings settings)
        {
            //Transactions must be enabled since FLR requires the transport to be able to rollback
            if (settings.GetRequiredTransactionModeForReceives() == TransportTransactionMode.None)
            {
                return false;
            }

            return GetMaxImmediateRetries(settings) > 0;
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

        int GetMaxImmediateRetries(ReadOnlySettings settings)
        {
            var retriesConfig = settings.GetConfigSection<TransportConfig>();

            return retriesConfig?.MaxRetries ?? settings.Get<int>(FlrNumberOfRetries);
        }

        public const string SlrNumberOfRetries = "Recoverability.Slr.DefaultPolicy.Retries";
        public const string SlrTimeIncrease = "Recoverability.Slr.DefaultPolicy.Timespan";
        public const string SlrCustomPolicy = "Recoverability.Slr.CustomPolicy";
        public const string FlrNumberOfRetries = "Recoverability.Flr.Retries";
        public const string FaultHeaderCustomization = "Recoverability.Failed.FaultHeaderCustomization";

        static ILog Logger = LogManager.GetLogger<Recoverability>();
    }

}