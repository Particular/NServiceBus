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
    using Settings;
    using Support;
    using Transports;

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

                settings.SetDefault(FailureInfoStorageCacheSizeKey, 1000);
            });
        }
        protected internal override void Setup(FeatureConfigurationContext context)
        {
            var errorQueue = context.Settings.ErrorQueueAddress();
            context.Settings.Get<QueueBindings>().BindSending(errorQueue);

            var transportTransactionMode = context.Settings.GetRequiredTransactionModeForReceives();
            var failureInfoStorage = new FailureInfoStorage(context.Settings.Get<int>(FailureInfoStorageCacheSizeKey));
            var localAddress = context.Settings.LocalAddress();

            context.Pipeline.Register("MoveFaultsToErrorQueue", b =>
            {
                var hostInfo = b.Build<HostInformation>();
                var staticFaultMetadata = new Dictionary<string, string>
                {
                    {FaultsHeaderKeys.FailedQ, localAddress},
                    {Headers.ProcessingMachine, RuntimeEnvironment.MachineName },
                    {Headers.ProcessingEndpoint, context.Settings.EndpointName()},
                    {Headers.HostId, hostInfo.HostId.ToString("N")},
                    {Headers.HostDisplayName, hostInfo.DisplayName}
                };

                var recoveryActionExecutor = new RecoveryActionExecutor(b.Build<IDispatchMessages>(), errorQueue, staticFaultMetadata);

                return new MoveFaultsToErrorQueueBehavior(b.Build<CriticalError>(),
                    transportTransactionMode,
                    failureInfoStorage,
                    recoveryActionExecutor);
            }, "Moves failing messages to the configured error queue");

            if (IsDelayedRetriesEnabled(context.Settings))
            {
                var retryPolicy = GetDelayedRetryPolicy(context.Settings);

                context.Pipeline.Register("SecondLevelRetries", b =>
                {
                    var delayedRetryExecutor = new DelayedRetryExecutor(
                        localAddress,
                        b.Build<IDispatchMessages>(),
                        context.DoesTransportSupportConstraint<DelayedDeliveryConstraint>()
                            ? null
                            : context.Settings.Get<TimeoutManagerAddressConfiguration>().TransportAddress);

                    return new SecondLevelRetriesBehavior(retryPolicy, failureInfoStorage, delayedRetryExecutor);
                }, "Performs second level retries");
            }

            if (IsImmediateRetriesEnabled(context.Settings))
            {
                var maxRetries = GetMaxRetries(context.Settings);
                var retryPolicy = new FirstLevelRetryPolicy(maxRetries);

                context.Pipeline.Register("FirstLevelRetries", b => new FirstLevelRetriesBehavior(failureInfoStorage, retryPolicy), "Performs first level retries");
            }

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

            return GetMaxRetries(settings) > 0;
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

        int GetMaxRetries(ReadOnlySettings settings)
        {
            var retriesConfig = settings.GetConfigSection<TransportConfig>();

            if (retriesConfig == null)
            {
                return settings.Get<int>(FlrNumberOfRetries);
            }

            return retriesConfig.MaxRetries;
        }

        public const string SlrNumberOfRetries = "Recoverability.Slr.DefaultPolicy.Retries";
        public const string SlrTimeIncrease = "Recoverability.Slr.DefaultPolicy.Timespan";
        public const string SlrCustomPolicy = "Recoverability.Slr.CustomPolicy";
        public const string FlrNumberOfRetries = "Recoverability.Flr.Retries";
        public const string FailureInfoStorageCacheSizeKey = "Recoverability.FailureInfoStorage.CacheSize";
    }
}