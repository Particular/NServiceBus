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
            bool raiseRecoverabilityNotifications,
            bool immediateRetriesAvailable,
            bool delayedRetriesAvailable,
            Func<RecoverabilityConfig, ErrorContext, RecoverabilityAction> recoverabilityPolicy,
            RecoverabilityConfig configuration,
            DelayedRetryExecutor delayedRetryExecutor,
            MoveToErrorsExecutor moveToErrorsExecutor,
            INotificationSubscriptions<MessageToBeRetried> messageRetryNotification,
            INotificationSubscriptions<MessageFaulted> messageFaultedNotification)
        {
            this.configuration = configuration;
            this.recoverabilityPolicy = recoverabilityPolicy;
            this.delayedRetryExecutor = delayedRetryExecutor;
            this.moveToErrorsExecutor = moveToErrorsExecutor;
            this.messageRetryNotification = messageRetryNotification;
            this.messageFaultedNotification = messageFaultedNotification;
            this.immediateRetriesAvailable = immediateRetriesAvailable;
            this.delayedRetriesAvailable = delayedRetriesAvailable;

            raiseNotifications = raiseRecoverabilityNotifications;
        }

        public Task<ErrorHandleResult> Invoke(ErrorContext errorContext, CancellationToken cancellationToken = default)
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
                return MoveToError(errorContext, configuration.Failed.ErrorQueue, cancellationToken);
            }

            if (recoveryAction is ImmediateRetry)
            {
                return RaiseImmediateRetryNotifications(errorContext, cancellationToken);
            }

            // When we can't do delayed retries, a policy customization probably didn't honor MaxNumberOfRetries for DelayedRetries
            if (recoveryAction is DelayedRetry && !delayedRetriesAvailable)
            {
                Logger.Warn("Recoverability policy requested DelayedRetry however delayed delivery capability is not available with the current endpoint configuration. Moving message to error queue instead.");
                return MoveToError(errorContext, configuration.Failed.ErrorQueue, cancellationToken);
            }

            if (recoveryAction is DelayedRetry delayedRetryAction)
            {
                return DeferMessage(delayedRetryAction, errorContext, cancellationToken);
            }

            if (recoveryAction is MoveToError moveToError)
            {
                return MoveToError(errorContext, moveToError.ErrorQueue, cancellationToken);
            }

            Logger.Warn("Recoverability policy returned an unsupported recoverability action. Moving message to error queue instead.");
            return MoveToError(errorContext, configuration.Failed.ErrorQueue, cancellationToken);
        }

        async Task<ErrorHandleResult> RaiseImmediateRetryNotifications(ErrorContext errorContext, CancellationToken cancellationToken = default)
        {
            Logger.Info($"Immediate Retry is going to retry message '{errorContext.Message.MessageId}' because of an exception:", errorContext.Exception);

            if (raiseNotifications)
            {
                await messageRetryNotification.Raise(
                        new MessageToBeRetried(
                            attempt: errorContext.ImmediateProcessingFailures - 1,
                            delay: TimeSpan.Zero,
                            immediateRetry: true,
                            errorContext: errorContext),
                        cancellationToken)
                    .ConfigureAwait(false);
            }

            return ErrorHandleResult.RetryRequired;
        }

        async Task<ErrorHandleResult> MoveToError(ErrorContext errorContext, string errorQueue, CancellationToken cancellationToken = default)
        {
            var message = errorContext.Message;

            Logger.Error($"Moving message '{message.MessageId}' to the error queue '{errorQueue}' because processing failed due to an exception:", errorContext.Exception);

            await moveToErrorsExecutor.MoveToErrorQueue(errorQueue, message, errorContext.Exception, errorContext.TransportTransaction, cancellationToken).ConfigureAwait(false);

            if (raiseNotifications)
            {
                await messageFaultedNotification.Raise(new MessageFaulted(errorContext, errorQueue), cancellationToken).ConfigureAwait(false);
            }

            return ErrorHandleResult.Handled;
        }

        async Task<ErrorHandleResult> DeferMessage(DelayedRetry action, ErrorContext errorContext, CancellationToken cancellationToken = default)
        {
            var message = errorContext.Message;

            Logger.Warn($"Delayed Retry will reschedule message '{message.MessageId}' after a delay of {action.Delay} because of an exception:", errorContext.Exception);

            var currentDelayedRetriesAttempts = await delayedRetryExecutor.Retry(message, action.Delay, errorContext.TransportTransaction, cancellationToken).ConfigureAwait(false);

            if (raiseNotifications)
            {
                await messageRetryNotification.Raise(
                        new MessageToBeRetried(
                            attempt: currentDelayedRetriesAttempts,
                            delay: action.Delay,
                            immediateRetry: false,
                            errorContext: errorContext),
                        cancellationToken)
                    .ConfigureAwait(false);
            }

            return ErrorHandleResult.Handled;
        }

        readonly INotificationSubscriptions<MessageToBeRetried> messageRetryNotification;
        readonly INotificationSubscriptions<MessageFaulted> messageFaultedNotification;
        Func<RecoverabilityConfig, ErrorContext, RecoverabilityAction> recoverabilityPolicy;
        DelayedRetryExecutor delayedRetryExecutor;
        MoveToErrorsExecutor moveToErrorsExecutor;
        bool raiseNotifications;
        bool immediateRetriesAvailable;
        bool delayedRetriesAvailable;
        RecoverabilityConfig configuration;

        static Task<ErrorHandleResult> HandledTask = Task.FromResult(ErrorHandleResult.Handled);
        static ILog Logger = LogManager.GetLogger<RecoverabilityExecutor>();
    }
}