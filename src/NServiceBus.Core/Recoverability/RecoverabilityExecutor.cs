namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Logging;
    using NServiceBus.Pipeline;
    using Recoverability;
    using Transport;

    class RecoverabilityExecutor
    {
        public RecoverabilityExecutor(
            DelayedRetryExecutor delayedRetryExecutor,
            MoveToErrorsExecutor moveToErrorsExecutor)
        {
            this.delayedRetryExecutor = delayedRetryExecutor;
            this.moveToErrorsExecutor = moveToErrorsExecutor;
        }

        public Task Invoke(IRecoverabilityContext recoverabilityContext)
        {
            var errorContext = recoverabilityContext.ErrorContext;
            var recoverabilityAction = recoverabilityContext.RecoverabilityAction;

            if (recoverabilityAction is Discard discard)
            {
                Logger.Info($"Discarding message with id '{errorContext.Message.MessageId}'. Reason: {discard.Reason}", errorContext.Exception);
                return Task.CompletedTask;
            }

            if (recoverabilityAction is ImmediateRetry)
            {
                Logger.Info($"Immediate Retry is going to retry message '{errorContext.Message.MessageId}' because of an exception:", errorContext.Exception);

                return Task.CompletedTask;
            }

            if (recoverabilityAction is DelayedRetry delayedRetryAction)
            {
                return DeferMessage(delayedRetryAction, recoverabilityContext);
            }

            if (recoverabilityAction is MoveToError moveToError)
            {
                return MoveToError(recoverabilityContext, moveToError.ErrorQueue);
            }

            throw new Exception("Cant't reach this");
        }

        async Task MoveToError(IRecoverabilityContext recoverabilityContext, string errorQueue)
        {
            var errorContext = recoverabilityContext.ErrorContext;
            var message = errorContext.Message;

            Logger.Error($"Moving message '{message.MessageId}' to the error queue '{errorQueue}' because processing failed due to an exception:", errorContext.Exception);

            await moveToErrorsExecutor.MoveToErrorQueue(
                errorQueue,
                errorContext,
                (transportOperation, token) =>
                {
                    return recoverabilityContext.Dispatch(new List<TransportOperation> { transportOperation });
                },
                recoverabilityContext.CancellationToken).ConfigureAwait(false);
        }

        async Task DeferMessage(DelayedRetry action, IRecoverabilityContext recoverabilityContext)
        {
            var errorContext = recoverabilityContext.ErrorContext;
            var message = errorContext.Message;

            Logger.Warn($"Delayed Retry will reschedule message '{message.MessageId}' after a delay of {action.Delay} because of an exception:", errorContext.Exception);

            await delayedRetryExecutor.Retry(
                errorContext,
                action.Delay,
                (transportOperation, token) =>
                {
                    return recoverabilityContext.Dispatch(new List<TransportOperation> { transportOperation });
                },
                recoverabilityContext.CancellationToken).ConfigureAwait(false);
        }

        DelayedRetryExecutor delayedRetryExecutor;
        MoveToErrorsExecutor moveToErrorsExecutor;

        static ILog Logger = LogManager.GetLogger<RecoverabilityExecutor>();
    }
}