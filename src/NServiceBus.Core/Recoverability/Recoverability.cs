namespace NServiceBus
{
    using System;
    using Config;
    using ConsistencyGuarantees;
    using Features;
    using Settings;
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
                settings.SetDefault(ImmedidateRetriesEnabled, true);
                settings.SetDefault(DelayedRetriesEnabled, true);
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

            var incomingStepSequence = context.Pipeline.Register("MoveFaultsToErrorQueue", b => new MoveFaultsToErrorQueueBehavior(
                b.Build<CriticalError>(),
                localAddress,
                transportTransactionMode,
                failureInfoStorage), "Moves failing messages to the configured error queue");  //context.Pipeline.Register(new MoveFaultsToErrorQueueBehavior.Registration(context.Settings.LocalAddress(), transportTransactionMode, failureInfoStorage));

            context.Pipeline.Register("AddExceptionHeaders", new AddExceptionHeadersBehavior(), "Adds the exception headers to the message");
            context.Pipeline.Register(new FaultToDispatchConnector(errorQueue), "Connector to dispatch faulted messages");

            if (IsDelayedRetriesEnabled(context.Settings))
            {
                var retryPolicy = GetDelayedRetryPolicy(context.Settings);

                incomingStepSequence.Register("SecondLevelRetries", b => new SecondLevelRetriesBehavior(retryPolicy, localAddress, failureInfoStorage), "Performs second level retries");
            }

            if (IsImmediateRetriesEnabled(context.Settings))
            {
                var transportConfig = context.Settings.GetConfigSection<TransportConfig>();
                var maxRetries = transportConfig?.MaxRetries ?? 5;
                var retryPolicy = new FirstLevelRetryPolicy(maxRetries);

                incomingStepSequence.Register("FirstLevelRetries", b => new FirstLevelRetriesBehavior(failureInfoStorage, retryPolicy), "Performs first level retries");
            }

            RaiseLegacyNotifications(context);
        }

        static SecondLevelRetryPolicy GetDelayedRetryPolicy(ReadOnlySettings settings)
        {
            var customRetryPolicy = settings.GetOrDefault<Func<IncomingMessage, TimeSpan>>("SecondLevelRetries.RetryPolicy");

            if (customRetryPolicy != null)
            {
                return new CustomSecondLevelRetryPolicy(customRetryPolicy);
            }

            var retriesConfig = settings.GetConfigSection<SecondLevelRetriesConfig>();
            if (retriesConfig != null)
            {
                return new DefaultSecondLevelRetryPolicy(retriesConfig.NumberOfRetries, retriesConfig.TimeIncrease);
            }

            return new DefaultSecondLevelRetryPolicy(DefaultSecondLevelRetryPolicy.DefaultNumberOfRetries, DefaultSecondLevelRetryPolicy.DefaultTimeIncrease);
        }

        bool IsDelayedRetriesEnabled(ReadOnlySettings settings)
        {
            if (!settings.Get<bool>(DelayedRetriesEnabled))
            {
                return false;
            }

            //Transactions must be enabled since SLR requires the transport to be able to rollback
            if (settings.GetRequiredTransactionModeForReceives() == TransportTransactionMode.None)
            {
                return false;
            }

            var retriesConfig = settings.GetConfigSection<SecondLevelRetriesConfig>();

            if (retriesConfig == null)
            {
                return true;
            }

            if (retriesConfig.NumberOfRetries == 0)
            {
                return false;
            }

            return retriesConfig.Enabled;
        }

        bool IsImmediateRetriesEnabled(ReadOnlySettings settings)
        {
            if (!settings.Get<bool>(ImmedidateRetriesEnabled))
            {
                return false;
            }

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
                return 5;
            }

            return retriesConfig.MaxRetries;
        }


        public const string ImmedidateRetriesEnabled = "Recoverability.ImmediateRetries.Enabled";
        public const string DelayedRetriesEnabled = "Recoverability.DelayedRetries.Enabled";

        const string FailureInfoStorageCacheSizeKey = "Recoverability.FailureInfoStorage.CacheSize";
    }
}