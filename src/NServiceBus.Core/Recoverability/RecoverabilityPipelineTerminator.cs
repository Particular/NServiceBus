namespace NServiceBus
{
    using System.Linq;
    using System.Threading.Tasks;
    using NServiceBus.Pipeline;
    using NServiceBus.Recoverability;

    class RecoverabilityPipelineTerminator : PipelineTerminator<IRecoverabilityContext>
    {
        public RecoverabilityPipelineTerminator(
            INotificationSubscriptions<MessageToBeRetried> messageRetryNotification,
            INotificationSubscriptions<MessageFaulted> messageFaultedNotification)
        {
            notifications = new CompositeNotification();
            notifications.Register(messageRetryNotification);
            notifications.Register(messageFaultedNotification);
        }
        protected override async Task Terminate(IRecoverabilityContext context)
        {
            var errorContext = context.ErrorContext;

            context.PreventChanges();

            RecoverabilityAction recoverabilityAction = context.RecoverabilityAction;
            var transportOperations = recoverabilityAction.GetTransportOperations(
                errorContext,
                context.Metadata);

            await context.Dispatch(transportOperations.ToList()).ConfigureAwait(false);

            var notification = recoverabilityAction.GetNotification(errorContext, context.Metadata);
            await notifications.Raise(notification, context.CancellationToken).ConfigureAwait(false);
        }

        readonly CompositeNotification notifications;
    }
}
