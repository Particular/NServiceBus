namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using Pipeline;

    class RecoverabilityRoutingConnector : StageConnector<IRecoverabilityContext, IRoutingContext>
    {
        public RecoverabilityRoutingConnector(
            INotificationSubscriptions<MessageToBeRetried> messageRetryNotification,
            INotificationSubscriptions<MessageFaulted> messageFaultedNotification)
        {
            notifications = new CompositeNotification();
            notifications.Register(messageRetryNotification);
            notifications.Register(messageFaultedNotification);
        }

        public override async Task Invoke(IRecoverabilityContext context, Func<IRoutingContext, Task> stage)
        {
            PreventInfiniteRetries(context);

            var recoverabilityActionContext = context.PreventChanges();

            RecoverabilityAction recoverabilityAction = context.RecoverabilityAction;
            var routingContexts = recoverabilityAction
                .GetRoutingContexts(recoverabilityActionContext);

            foreach (var routingContext in routingContexts)
            {
                await stage(routingContext).ConfigureAwait(false);
            }

            if (context is IRecoverabilityActionContextNotifications events)
            {
                foreach (object @event in events)
                {
                    await notifications.Raise(@event, context.CancellationToken).ConfigureAwait(false);
                }
            }
        }

        static void PreventInfiniteRetries(IRecoverabilityContext context)
        {
            var totalRetries = (context.ImmediateProcessingFailures - 1) * (context.DelayedDeliveriesPerformed + 1);

            //TODO: should probably use a fallback of ~10 when the configuration is explicitly configured to not do retries to not constantly enforce the MaxRetries setting in such cases.
            var totalAllowedRetries = context.MaximumRetries ??
                                      context.RecoverabilityConfiguration.Immediate.MaxNumberOfRetries *
                                      (context.RecoverabilityConfiguration.Delayed.MaxNumberOfRetries + 1);

            if (totalRetries >= totalAllowedRetries)
            {
                if (context.RecoverabilityAction is ImmediateRetry or DelayedRetry)
                {
                    context.RecoverabilityAction =
                        RecoverabilityAction.MoveToError(context.RecoverabilityConfiguration.Failed.ErrorQueue);
                }
            }
        }

        readonly CompositeNotification notifications;
    }
}
