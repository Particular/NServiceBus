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
            bool immediateRetriesAvailable,
            bool delayedRetriesAvailable,
            Func<RecoverabilityConfig, ErrorContext, RecoverabilityAction> recoverabilityPolicy,
            RecoverabilityConfig configuration,
            DelayedRetryExecutor delayedRetryExecutor,
            MoveToErrorsExecutor moveToErrorsExecutor)
        {
            this.configuration = configuration;
            this.recoverabilityPolicy = recoverabilityPolicy;
            this.delayedRetryExecutor = delayedRetryExecutor;
            this.moveToErrorsExecutor = moveToErrorsExecutor;
            this.immediateRetriesAvailable = immediateRetriesAvailable;
            this.delayedRetriesAvailable = delayedRetriesAvailable;
        }

        public Task<ErrorHandleResult> Invoke(
            ErrorContext errorContext,
            IMessageDispatcher messageDispatcher,
            CancellationToken cancellationToken = default)
        {
            var recoveryAction = recoverabilityPolicy(configuration, errorContext);

            if (recoveryAction is Discard discard)
            {
                Logger.Info($"Discarding message with id '{errorContext.Message.MessageId}'. Reason: {discard.Reason}", errorContext.Exception);
                return HandledTask;
            }

            // When we can't do immediate retries and policy did not honor MaxNumberOfRetries for ImmediateRetries
            if (recoveryAction is ImmediateRetry && !immediateRetriesAvailable)
            {
                Logger.Warn("Recoverability policy requested ImmediateRetry however immediate retries are not available with the current endpoint configuration. Moving message to error queue instead.");
                return MoveToError(errorContext, configuration.Failed.ErrorQueue, messageDispatcher, cancellationToken);
            }

            if (recoveryAction is ImmediateRetry)
            {
                return RaiseImmediateRetryNotifications(errorContext, cancellationToken);
            }

            // When we can't do delayed retries, a policy customization probably didn't honor MaxNumberOfRetries for DelayedRetries
            if (recoveryAction is DelayedRetry && !delayedRetriesAvailable)
            {
                Logger.Warn("Recoverability policy requested DelayedRetry however delayed delivery capability is not available with the current endpoint configuration. Moving message to error queue instead.");
                return MoveToError(errorContext, configuration.Failed.ErrorQueue, messageDispatcher, cancellationToken);
            }

            if (recoveryAction is DelayedRetry delayedRetryAction)
            {
                return DeferMessage(delayedRetryAction, errorContext, messageDispatcher, cancellationToken);
            }

            if (recoveryAction is MoveToError moveToError)
            {
                return MoveToError(errorContext, moveToError.ErrorQueue, messageDispatcher, cancellationToken);
            }

            Logger.Warn("Recoverability policy returned an unsupported recoverability action. Moving message to error queue instead.");
            return MoveToError(errorContext, configuration.Failed.ErrorQueue, messageDispatcher, cancellationToken);
        }

        Task<ErrorHandleResult> RaiseImmediateRetryNotifications(ErrorContext errorContext, CancellationToken cancellationToken)
        {
            Logger.Info($"Immediate Retry is going to retry message '{errorContext.Message.MessageId}' because of an exception:", errorContext.Exception);

            return Task.FromResult(ErrorHandleResult.RetryRequired);
        }

        async Task<ErrorHandleResult> MoveToError(ErrorContext errorContext, string errorQueue, IMessageDispatcher messageDispatcher, CancellationToken cancellationToken)
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

            return ErrorHandleResult.Handled;
        }

        async Task<ErrorHandleResult> DeferMessage(DelayedRetry action, ErrorContext errorContext, IMessageDispatcher messageDispatcher, CancellationToken cancellationToken)
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

            return ErrorHandleResult.Handled;
        }

        Func<RecoverabilityConfig, ErrorContext, RecoverabilityAction> recoverabilityPolicy;
        DelayedRetryExecutor delayedRetryExecutor;
        MoveToErrorsExecutor moveToErrorsExecutor;
        bool immediateRetriesAvailable;
        bool delayedRetriesAvailable;
        RecoverabilityConfig configuration;

        static Task<ErrorHandleResult> HandledTask = Task.FromResult(ErrorHandleResult.Handled);
        static ILog Logger = LogManager.GetLogger<RecoverabilityExecutor>();
    }
}