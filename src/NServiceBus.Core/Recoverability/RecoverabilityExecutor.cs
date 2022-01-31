namespace NServiceBus
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Logging;
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

        public Task Invoke(
            ErrorContext errorContext,
            RecoverabilityAction recoverabilityAction,
            Func<TransportOperation, CancellationToken, Task> dispatchAction,
            CancellationToken cancellationToken = default)
        {
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
                var message = errorContext.Message;
                var delay = delayedRetryAction.Delay;

                Logger.Warn($"Delayed Retry will reschedule message '{message.MessageId}' after a delay of {delay} because of an exception:", errorContext.Exception);

                return delayedRetryExecutor.Retry(
                    errorContext,
                    delay,
                    dispatchAction,
                    cancellationToken);
            }

            if (recoverabilityAction is MoveToError moveToError)
            {
                var message = errorContext.Message;
                var errorQueue = moveToError.ErrorQueue;

                Logger.Error($"Moving message '{message.MessageId}' to the error queue '{errorQueue}' because processing failed due to an exception:", errorContext.Exception);

                return moveToErrorsExecutor.MoveToErrorQueue(
                    errorQueue,
                    errorContext,
                    dispatchAction,
                    cancellationToken);
            }

            throw new Exception("Cant't reach this");
        }

        DelayedRetryExecutor delayedRetryExecutor;
        MoveToErrorsExecutor moveToErrorsExecutor;

        static ILog Logger = LogManager.GetLogger<RecoverabilityExecutor>();
    }
}