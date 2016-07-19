namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using Logging;
    using Transport;

    class RecoverabilityExecutor
    {
        public RecoverabilityExecutor(bool raiseRecoverabilityNotifications, bool immediateRetriesAvailable, bool delayedRetriesAvailable, Func<RecoverabilityConfig, ErrorContext, RecoverabilityAction> recoverabilityPolicy, RecoverabilityConfig configuration, IEventAggregator eventAggregator, DelayedRetryExecutor delayedRetryExecutor, MoveToErrorsExecutor moveToErrorsExecutor)
        {
            this.configuration = configuration;
            this.recoverabilityPolicy = recoverabilityPolicy;
            this.eventAggregator = eventAggregator;
            this.delayedRetryExecutor = delayedRetryExecutor;
            this.moveToErrorsExecutor = moveToErrorsExecutor;
            this.immediateRetriesAvailable = immediateRetriesAvailable;
            this.delayedRetriesAvailable = delayedRetriesAvailable;

            raiseNotifications = raiseRecoverabilityNotifications;
        }

        public Task<ErrorHandleResult> Invoke(ErrorContext errorContext)
        {
            var recoveryAction = recoverabilityPolicy(configuration, errorContext);

            // When we can't do immediate retries and policy did not honor MaxNumberOfRetries for ImmediateRetries
            if (recoveryAction is ImmediateRetry && !immediateRetriesAvailable)
            {
                Logger.Warn("Recoverability policy requested ImmediateRetry however immediate retires are not available with current endpoint configuration. Moving message to error queue instead.");
                return MoveToError(errorContext, configuration.Failed.ErrorQueue);
            }

            if (recoveryAction is ImmediateRetry)
            {
                return RaiseImmediateRetryNotifications(errorContext);
            }

            // When we can't do delayed retries, a policy customization probably didn't honor MaxNumberOfRetries for DelayedRetries
            if (recoveryAction is DelayedRetry && !delayedRetriesAvailable)
            {
                Logger.Warn("Recoverability policy requested DelayedRetry however delayed delivery capability is not available with current endpoint configuration. Moving message to error queue instead.");
                return MoveToError(errorContext, configuration.Failed.ErrorQueue);
            }

            var delayedRetryAction = recoveryAction as DelayedRetry;
            if (delayedRetryAction != null)
            {
                return DeferMessage(delayedRetryAction, errorContext);
            }

            var moveToError = recoveryAction as MoveToError;
            if (moveToError != null)
            {
                return MoveToError(errorContext, moveToError.ErrorQueue);
            }

            throw new Exception("Unknown recoverability action returned from RecoverabilityPolicy");
        }

        async Task<ErrorHandleResult> RaiseImmediateRetryNotifications(ErrorContext errorContext)
        {
            var message = errorContext.Message;

            Logger.Info($"First Level Retry is going to retry message '{message.MessageId}' because of an exception:", errorContext.Exception);

            if (raiseNotifications)
            {
                await eventAggregator.Raise(new MessageToBeRetried(errorContext.ImmediateProcessingFailures - 1, TimeSpan.Zero, message, errorContext.Exception)).ConfigureAwait(false);
            }

            return ErrorHandleResult.RetryRequired;
        }

        async Task<ErrorHandleResult> MoveToError(ErrorContext errorContext, string errorQueue)
        {
            var message = errorContext.Message;

            Logger.Error($"Moving message '{message.MessageId}' to the error queue '{ errorQueue }' because processing failed due to an exception:", errorContext.Exception);

            await moveToErrorsExecutor.MoveToErrorQueue(errorQueue, message, errorContext.Exception, errorContext.TransportTransaction).ConfigureAwait(false);

            if (raiseNotifications)
            {
                await eventAggregator.Raise(new MessageFaulted(message, errorContext.Exception)).ConfigureAwait(false);
            }
            return ErrorHandleResult.Handled;
        }

        async Task<ErrorHandleResult> DeferMessage(DelayedRetry action, ErrorContext errorContext)
        {
            var message = errorContext.Message;

            Logger.Warn($"Second Level Retry will reschedule message '{message.MessageId}' after a delay of {action.Delay} because of an exception:", errorContext.Exception);

            var currentSlrAttempts = await delayedRetryExecutor.Retry(message, action.Delay, errorContext.TransportTransaction).ConfigureAwait(false);

            if (raiseNotifications)
            {
                await eventAggregator.Raise(new MessageToBeRetried(currentSlrAttempts, action.Delay, message, errorContext.Exception)).ConfigureAwait(false);
            }
            return ErrorHandleResult.Handled;
        }

        Func<RecoverabilityConfig, ErrorContext, RecoverabilityAction> recoverabilityPolicy;
        IEventAggregator eventAggregator;
        DelayedRetryExecutor delayedRetryExecutor;
        MoveToErrorsExecutor moveToErrorsExecutor;
        bool raiseNotifications;
        bool immediateRetriesAvailable;
        bool delayedRetriesAvailable;

        static ILog Logger = LogManager.GetLogger<RecoverabilityExecutor>();
        RecoverabilityConfig configuration;
    }
}