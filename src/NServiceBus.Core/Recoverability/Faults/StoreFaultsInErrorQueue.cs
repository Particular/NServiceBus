namespace NServiceBus.Features
{
    using Transports;

    class StoreFaultsInErrorQueue : Feature
    {
        public StoreFaultsInErrorQueue()
        {
            EnableByDefault();
            Prerequisite(context =>
            {
                var b = !context.Settings.GetOrDefault<bool>("Endpoint.SendOnly");
                return b;
            }, "Send only endpoints can't be used to forward received messages to the error queue as the endpoint requires receive capabilities");
        }

        protected internal override void Setup(FeatureConfigurationContext context)
        {
            var errorQueue = ErrorQueueSettings.GetConfiguredErrorQueue(context.Settings);
            context.Settings.Get<QueueBindings>().BindSending(errorQueue);
            context.Pipeline.Register(new MoveFaultsToErrorQueueBehavior.Registration(errorQueue, context.Settings.LocalAddress()));
            context.Pipeline.Register("FaultToDispatchConnector", new FaultToDispatchConnector(), "Connector to dispatch faulted messages");

            RaiseLegacyBusNotifications(context);
        }

        //note: will soon be removed since we're deprecating BusNotifications in favor of the new notifications
        static void RaiseLegacyBusNotifications(FeatureConfigurationContext context)
        {
            var busNotifications = context.Settings.Get<BusNotifications>();
            var notifications = context.Settings.Get<NotificationSubscriptions>();

            notifications.Subscribe<MessageToBeRetried>(e =>
            {
                if (e.IsImmediateRetry)
                {
                    busNotifications.Errors.InvokeMessageHasFailedAFirstLevelRetryAttempt(e.Attempt, e.Message, e.Exception);
                }
                else
                {
                    busNotifications.Errors.InvokeMessageHasBeenSentToSecondLevelRetries(e.Attempt, e.Message, e.Exception);
                }

                return TaskEx.CompletedTask;
            });

            notifications.Subscribe<MessageFaulted>(e =>
            {
                busNotifications.Errors.InvokeMessageHasBeenSentToErrorQueue(e.Message, e.Exception);
                return TaskEx.CompletedTask;
            });
        }
    }
}