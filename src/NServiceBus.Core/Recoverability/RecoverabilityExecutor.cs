﻿namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using Logging;
    using Transport;

    class RecoverabilityExecutor
    {
        public RecoverabilityExecutor(bool raiseRecoverabilityNotifications, Func<RecoverabilityConfig, ErrorContext, RecoverabilityAction> recoverabilityPolicy, RecoverabilityConfig configuration, IEventAggregator eventAggregator, DelayedRetryExecutor delayedRetryExecutor, MoveToErrorsExecutor moveToErrorsExecutor)
        {
            this.configuration = configuration;
            this.recoverabilityPolicy = recoverabilityPolicy;
            this.eventAggregator = eventAggregator;
            this.delayedRetryExecutor = delayedRetryExecutor;
            this.moveToErrorsExecutor = moveToErrorsExecutor;

            raiseNotifications = raiseRecoverabilityNotifications;
            delayedRetriesCapabilityAvailable = delayedRetryExecutor != null;
        }

        public Task<ErrorHandleResult> Invoke(ErrorContext errorContext)
        {
            var recoveryAction = recoverabilityPolicy.Invoke(configuration, errorContext);

            if (recoveryAction is ImmediateRetry)
            {
                return RaiseImmediateRetryNotifications(errorContext);
            }

            // When we can't do delayed retries then just fall through to error.
            if (recoveryAction is DelayedRetry && !delayedRetriesCapabilityAvailable)
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

            Logger.Info($"First Level Retry is going to retry message '{message.MessageId}' because of an exception:", errorContext.Exception);

            if (raiseNotifications)
            {
                await eventAggregator.Raise(new MessageToBeRetried(errorContext.NumberOfImmediateDeliveryAttempts - 1, TimeSpan.Zero, message, errorContext.Exception)).ConfigureAwait(false);
            }

            return ErrorHandleResult.RetryRequired;
        }

        async Task<ErrorHandleResult> MoveToError(ErrorContext errorContext)
        {
            var message = errorContext.Message;

            Logger.Error($"Moving message '{message.MessageId}' to the error queue because processing failed due to an exception:", errorContext.Exception);

            await moveToErrorsExecutor.MoveToErrorQueue(message, errorContext.Exception, errorContext.TransportTransaction).ConfigureAwait(false);

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

        static ILog Logger = LogManager.GetLogger<RecoverabilityExecutor>();
        RecoverabilityConfig configuration;
        bool delayedRetriesCapabilityAvailable;
    }
}