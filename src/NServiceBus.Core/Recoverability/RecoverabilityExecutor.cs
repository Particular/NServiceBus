namespace NServiceBus
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Logging;
    using NServiceBus.Pipeline;
    using Transport;
    using Recoverability;
    using System.Collections.Generic;

    class RecoverabilityExecutor
    {
        public RecoverabilityExecutor(
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
        }

        public Task<ErrorHandleResult> Invoke(IRecoverabilityContext recoverabilityContext)
        {
            var errorContext = recoverabilityContext.ErrorContext;
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
                return MoveToError(recoverabilityContext, configuration.Failed.ErrorQueue);
            }

            if (recoveryAction is ImmediateRetry)
            {
                return RaiseImmediateRetryNotifications(errorContext, recoverabilityContext.CancellationToken);
            }

            // When we can't do delayed retries, a policy customization probably didn't honor MaxNumberOfRetries for DelayedRetries
            if (recoveryAction is DelayedRetry && !delayedRetriesAvailable)
            {
                Logger.Warn("Recoverability policy requested DelayedRetry however delayed delivery capability is not available with the current endpoint configuration. Moving message to error queue instead.");
                return MoveToError(recoverabilityContext, configuration.Failed.ErrorQueue);
            }

            if (recoveryAction is DelayedRetry delayedRetryAction)
            {
                return DeferMessage(delayedRetryAction, recoverabilityContext);
            }

            if (recoveryAction is MoveToError moveToError)
            {
                return MoveToError(recoverabilityContext, moveToError.ErrorQueue);
            }

            Logger.Warn("Recoverability policy returned an unsupported recoverability action. Moving message to error queue instead.");
            return MoveToError(recoverabilityContext, configuration.Failed.ErrorQueue);
        }

        async Task<ErrorHandleResult> RaiseImmediateRetryNotifications(ErrorContext errorContext, CancellationToken cancellationToken)
        {
            Logger.Info($"Immediate Retry is going to retry message '{errorContext.Message.MessageId}' because of an exception:", errorContext.Exception);

            await messageRetryNotification.Raise(
                    new MessageToBeRetried(
                        attempt: errorContext.ImmediateProcessingFailures - 1,
                        delay: TimeSpan.Zero,
                        immediateRetry: true,
                        errorContext: errorContext),
                    cancellationToken)
                .ConfigureAwait(false);

            return ErrorHandleResult.RetryRequired;
        }

        async Task<ErrorHandleResult> MoveToError(IRecoverabilityContext recoverabilityContext, string errorQueue)
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

            await messageFaultedNotification.Raise(new MessageFaulted(errorContext, errorQueue), recoverabilityContext.CancellationToken).ConfigureAwait(false);

            return ErrorHandleResult.Handled;
        }

        async Task<ErrorHandleResult> DeferMessage(DelayedRetry action, IRecoverabilityContext recoverabilityContext)
        {
            var errorContext = recoverabilityContext.ErrorContext;
            var message = errorContext.Message;

            Logger.Warn($"Delayed Retry will reschedule message '{message.MessageId}' after a delay of {action.Delay} because of an exception:", errorContext.Exception);

            var currentDelayedRetriesAttempts = await delayedRetryExecutor.Retry(
                errorContext,
                action.Delay,
                (transportOperation, token) =>
                {
                    return recoverabilityContext.Dispatch(new List<TransportOperation> { transportOperation });
                },
                recoverabilityContext.CancellationToken).ConfigureAwait(false);

            await messageRetryNotification.Raise(
                    new MessageToBeRetried(
                        attempt: currentDelayedRetriesAttempts,
                        delay: action.Delay,
                        immediateRetry: false,
                        errorContext: errorContext),
                    recoverabilityContext.CancellationToken)
                .ConfigureAwait(false);

            return ErrorHandleResult.Handled;
        }

        readonly INotificationSubscriptions<MessageToBeRetried> messageRetryNotification;
        readonly INotificationSubscriptions<MessageFaulted> messageFaultedNotification;
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