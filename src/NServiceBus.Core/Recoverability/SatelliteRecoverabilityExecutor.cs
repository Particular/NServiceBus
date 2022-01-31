namespace NServiceBus
{
    using System.Threading;
    using System.Threading.Tasks;
    using Logging;
    using Transport;

    class SatelliteRecoverabilityExecutor
    {
        public SatelliteRecoverabilityExecutor(
            bool immediateRetriesAvailable,
            bool delayedRetriesAvailable,
            RecoverabilityConfig configuration,
            DelayedRetryExecutor delayedRetryExecutor,
            MoveToErrorsExecutor moveToErrorsExecutor)
        {
            this.configuration = configuration;
            this.delayedRetryExecutor = delayedRetryExecutor;
            this.moveToErrorsExecutor = moveToErrorsExecutor;
            this.immediateRetriesAvailable = immediateRetriesAvailable;
            this.delayedRetriesAvailable = delayedRetriesAvailable;
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

            // When we can't do immediate retries and policy did not honor MaxNumberOfRetries for ImmediateRetries
            if (recoverabilityAction is ImmediateRetry && !immediateRetriesAvailable)
            {
                Logger.Warn("Recoverability policy requested ImmediateRetry however immediate retries are not available with the current endpoint configuration. Moving message to error queue instead.");
                return MoveToError(errorContext, configuration.Failed.ErrorQueue, messageDispatcher, cancellationToken);
            }

            if (recoverabilityAction is ImmediateRetry)
            {
                Logger.Info($"Immediate Retry is going to retry message '{errorContext.Message.MessageId}' because of an exception:", errorContext.Exception);

                return Task.CompletedTask;
            }

            // When we can't do delayed retries, a policy customization probably didn't honor MaxNumberOfRetries for DelayedRetries
            if (recoverabilityAction is DelayedRetry && !delayedRetriesAvailable)
            {
                Logger.Warn("Recoverability policy requested DelayedRetry however delayed delivery capability is not available with the current endpoint configuration. Moving message to error queue instead.");
                return MoveToError(errorContext, configuration.Failed.ErrorQueue, messageDispatcher, cancellationToken);
            }

            if (recoverabilityAction is DelayedRetry delayedRetryAction)
            {
                return DeferMessage(delayedRetryAction, errorContext, messageDispatcher, cancellationToken);
            }

            if (recoverabilityAction is MoveToError moveToError)
            {
                return MoveToError(errorContext, moveToError.ErrorQueue, messageDispatcher, cancellationToken);
            }

            Logger.Warn("Recoverability policy returned an unsupported recoverability action. Moving message to error queue instead.");
            return MoveToError(errorContext, configuration.Failed.ErrorQueue, messageDispatcher, cancellationToken);
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
        bool immediateRetriesAvailable;
        bool delayedRetriesAvailable;
        RecoverabilityConfig configuration;

        static ILog Logger = LogManager.GetLogger<RecoverabilityExecutor>();
    }
}