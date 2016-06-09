namespace NServiceBus
{
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

            Prerequisite(context => !context.Settings.GetOrDefault<bool>("Endpoint.SendOnly"),
                "Message recoverability is only relevant for endpoints receiving messages.");
            Prerequisite(context => context.Settings.GetRequiredTransactionModeForReceives() != TransportTransactionMode.None, "Transactions must be enabled since FLR requires the transport to be able to rollback");
            Prerequisite(context => GetMaxRetries(context.Settings) > 0, "FLR was disabled in config since it's set to 0");
            Defaults(settings => { settings.SetDefault(FailureInfoStorageCacheSizeKey, 1000); });

        }
        protected internal override void Setup(FeatureConfigurationContext context)
        {
            var errorQueue = context.Settings.ErrorQueueAddress();
            context.Settings.Get<QueueBindings>().BindSending(errorQueue);

            var transportTransactionMode = context.Settings.GetRequiredTransactionModeForReceives();
            context.Pipeline.Register(new MoveFaultsToErrorQueueBehavior.Registration(context.Settings.LocalAddress(), transportTransactionMode));
            context.Pipeline.Register("AddExceptionHeaders", new AddExceptionHeadersBehavior(), "Adds the exception headers to the message");
            context.Pipeline.Register(new FaultToDispatchConnector(errorQueue), "Connector to dispatch faulted messages");


            var transportConfig = context.Settings.GetConfigSection<TransportConfig>();
            var maxRetries = transportConfig?.MaxRetries ?? 5;
            var retryPolicy = new FirstLevelRetryPolicy(maxRetries);

            var failureInfoStorage = new FailureInfoStorage(context.Settings.Get<int>(FailureInfoStorageCacheSizeKey));

            context.Pipeline.Register("FirstLevelRetries", b => new FirstLevelRetriesBehavior(failureInfoStorage, retryPolicy), "Performs first level retries");

            RaiseLegacyNotifications(context);
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


        const string FailureInfoStorageCacheSizeKey = "Recoverability.FailureInfoStorage.CacheSize";
    }
}