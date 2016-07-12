namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using Logging;

    class RecoverabilityExecutor
    {
        //TODO: figure out proper logging for ExceptionInfo
        public RecoverabilityExecutor(IRecoverabilityPolicy recoverabilityPolicy, IEventAggregator eventAggregator, DelayedRetryExecutor delayedRetryExecutor, MoveToErrorsExecutor moveToErrorsExecutor, bool transactionsOn)
        {
            this.recoverabilityPolicy = recoverabilityPolicy;
            this.eventAggregator = eventAggregator;
            this.delayedRetryExecutor = delayedRetryExecutor;
            this.moveToErrorsExecutor = moveToErrorsExecutor;
            this.transactionsOn = transactionsOn;

            raiseNotifications = recoverabilityPolicy.RaiseRecoverabilityNotifications;
            delayedRetriesEnabled = delayedRetryExecutor != null;
        }

        public Task<ErrorHandleResult> Invoke(ErrorContext errorContext)
        {
            if (transactionsOn == false || 
                errorContext.ExceptionInfo.TypeFullName == typeof(MessageDeserializationException).FullName)
            {
                return MoveToError(errorContext);
            }

            return PerformRecoverabilityAction(errorContext);
        }

        Task<ErrorHandleResult> PerformRecoverabilityAction(ErrorContext errorContext)
        {
            var recoveryAction = recoverabilityPolicy.Invoke(errorContext);

            if (recoveryAction is ImmediateRetry)
            {
                return RaiseImmediateRetryNotifications(errorContext);
            }

            // When we can't do delayed retries then just fall through to error.
            if (recoveryAction is DelayedRetry && !delayedRetriesEnabled)
            {
                Logger.Warn("Current recoverability policy requested delayed retry but delayed delivery is not supported by this endpoint. Consider enabling the timeout manager or use a transport which natively supports delayed delivery. Moving to the error queue instead.");
                recoveryAction = RecoverabilityAction.MoveToError();
            }

            var delayedRetryAction = recoveryAction as DelayedRetry;
            if (delayedRetryAction != null)
            {
                return DeferMessage(delayedRetryAction, errorContext);
            }

            if (recoveryAction is MoveToError)
            {
                return MoveToError(errorContext);
            }

            throw new Exception("Unknown recoverability action returned from RecoverabilityPolicy");
        }

        async Task<ErrorHandleResult> RaiseImmediateRetryNotifications(ErrorContext errorContext)
        {
            var message = errorContext.Message;

            Logger.Info($"First Level Retry is going to retry message '{message.MessageId}' because of an exception:" + Environment.NewLine + errorContext.ExceptionInfo.StackTrace);

            if (raiseNotifications)
            {
                await eventAggregator.Raise(new MessageToBeRetried(errorContext.NumberOfDeliveryAttempts - 1, TimeSpan.Zero, message, errorContext.ExceptionInfo)).ConfigureAwait(false);
            }

            return ErrorHandleResult.RetryRequired;
        }

        async Task<ErrorHandleResult> MoveToError(ErrorContext errorContext)
        {
            var message = errorContext.Message;

            Logger.Error($"Moving message '{message.MessageId}' to the error queue because processing failed due to an exception:" + Environment.NewLine + errorContext.ExceptionInfo.StackTrace);

            await moveToErrorsExecutor.MoveToErrorQueue(message, errorContext.ExceptionInfo, errorContext.TransportTransaction).ConfigureAwait(false);

            if (raiseNotifications)
            {
                await eventAggregator.Raise(new MessageFaulted(message, errorContext.ExceptionInfo)).ConfigureAwait(false);
            }
            return ErrorHandleResult.Handled;
        }

        async Task<ErrorHandleResult> DeferMessage(DelayedRetry action, ErrorContext errorContext)
        {
            var message = errorContext.Message;

            Logger.Warn($"Second Level Retry will reschedule message '{message.MessageId}' after a delay of {action.Delay} because of an exception:" + Environment.NewLine + errorContext.ExceptionInfo.StackTrace);

            var currentSlrAttempts = await delayedRetryExecutor.Retry(message, action.Delay, errorContext.TransportTransaction).ConfigureAwait(false);

            if (raiseNotifications)
            {
                await eventAggregator.Raise(new MessageToBeRetried(currentSlrAttempts, action.Delay, message, errorContext.ExceptionInfo)).ConfigureAwait(false);
            }
            return ErrorHandleResult.Handled;
        }

        IRecoverabilityPolicy recoverabilityPolicy;
        IEventAggregator eventAggregator;
        DelayedRetryExecutor delayedRetryExecutor;
        MoveToErrorsExecutor moveToErrorsExecutor;
        bool transactionsOn;
        bool raiseNotifications;
        bool delayedRetriesEnabled;

        static ILog Logger = LogManager.GetLogger<RecoverabilityExecutor>();
    }
}