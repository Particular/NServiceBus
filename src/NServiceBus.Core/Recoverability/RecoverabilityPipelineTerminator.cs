namespace NServiceBus
{
    using System;
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
            this.messageRetryNotification = messageRetryNotification;
            this.messageFaultedNotification = messageFaultedNotification;
        }
        protected override async Task Terminate(IRecoverabilityContext context)
        {
            var errorContext = context.ErrorContext;

            context.PreventChanges();

            var transportOperations = context.RecoverabilityAction.Execute(
                errorContext,
                context.Metadata);

            await context.Dispatch(transportOperations.ToList()).ConfigureAwait(false);

            if (context.RecoverabilityAction is ImmediateRetry)
            {
                var messageToBeRetriedEvent = new MessageToBeRetried(
                                attempt: errorContext.ImmediateProcessingFailures - 1,
                                delay: TimeSpan.Zero,
                                immediateRetry: true,
                                errorContext: errorContext);

                await messageRetryNotification.Raise(messageToBeRetriedEvent, context.CancellationToken)
                    .ConfigureAwait(false);
            }

            if (context.RecoverabilityAction is DelayedRetry)
            {
                var delayAction = context.RecoverabilityAction as DelayedRetry;
                var currentDelayedRetriesAttempts = context.ErrorContext.Message.GetDelayedDeliveriesPerformed() + 1;

                var messageToBeRetriedEvent = new MessageToBeRetried(
                            attempt: currentDelayedRetriesAttempts,
                            delay: delayAction.Delay,
                            immediateRetry: false,
                            errorContext: errorContext);

                await messageRetryNotification.Raise(messageToBeRetriedEvent, context.CancellationToken)
                    .ConfigureAwait(false);
            }

            if (context.RecoverabilityAction is MoveToError)
            {
                var errorAction = context.RecoverabilityAction as MoveToError;
                var messageFaultedEvent = new MessageFaulted(errorContext, errorAction.ErrorQueue);

                await messageFaultedNotification.Raise(messageFaultedEvent, context.CancellationToken)
                    .ConfigureAwait(false);
            }
        }

        readonly INotificationSubscriptions<MessageToBeRetried> messageRetryNotification;
        readonly INotificationSubscriptions<MessageFaulted> messageFaultedNotification;
    }
}
