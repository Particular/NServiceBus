namespace NServiceBus
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Logging;
    using Transport;

    class SatelliteRecoverabilityExecutor
    {
        public SatelliteRecoverabilityExecutor(
            DelayedRetryExecutor delayedRetryExecutor,
            MoveToErrorsExecutor moveToErrorsExecutor)
        {
            this.delayedRetryExecutor = delayedRetryExecutor;
            this.moveToErrorsExecutor = moveToErrorsExecutor;
        }

        public Task Invoke(
            ErrorContext errorContext,
            IMessageDispatcher messageDispatcher,
            RecoverabilityAction recoverabilityAction,
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
                return DeferMessage(delayedRetryAction, errorContext, messageDispatcher, cancellationToken);
            }

            if (recoverabilityAction is MoveToError moveToError)
            {
                return MoveToError(errorContext, moveToError.ErrorQueue, messageDispatcher, cancellationToken);
            }

            throw new Exception("Cant't reach this");
        }

        async Task MoveToError(ErrorContext errorContext, string errorQueue, IMessageDispatcher messageDispatcher, CancellationToken cancellationToken)
        {
            var message = errorContext.Message;

            Logger.Error($"Moving message '{message.MessageId}' to the error queue '{errorQueue}' because processing failed due to an exception:", errorContext.Exception);

            await moveToErrorsExecutor.MoveToErrorQueue(
                errorQueue,
                errorContext,
                (transportOperation, token) =>
                {
                    return messageDispatcher.Dispatch(new TransportOperations(transportOperation), errorContext.TransportTransaction, token);
                },
                cancellationToken).ConfigureAwait(false);
        }

        async Task DeferMessage(DelayedRetry action, ErrorContext errorContext, IMessageDispatcher messageDispatcher, CancellationToken cancellationToken)
        {
            var message = errorContext.Message;

            Logger.Warn($"Delayed Retry will reschedule message '{message.MessageId}' after a delay of {action.Delay} because of an exception:", errorContext.Exception);

            await delayedRetryExecutor.Retry(
                errorContext,
                action.Delay,
                (transportOperation, token) =>
                {
                    return messageDispatcher.Dispatch(new TransportOperations(transportOperation), errorContext.TransportTransaction, token);
                },
                cancellationToken).ConfigureAwait(false);
        }

        DelayedRetryExecutor delayedRetryExecutor;
        MoveToErrorsExecutor moveToErrorsExecutor;

        static ILog Logger = LogManager.GetLogger<RecoverabilityExecutor>();
    }
}