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

        readonly CompositeNotification notifications;
    }
}
