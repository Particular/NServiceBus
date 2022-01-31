namespace NServiceBus
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Logging;
    using NServiceBus.Pipeline;
    using Recoverability;
    using Transport;

    class RecoverabilityExecutor
    {
        public RecoverabilityExecutor(
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

        public Task Invoke(IRecoverabilityContext recoverabilityContext)
        {
            var errorContext = recoverabilityContext.ErrorContext;
            var recoverabilityAction = recoverabilityContext.RecoverabilityAction;

            if (recoverabilityAction is Discard discard)
            {
                Logger.Info($"Discarding message with id '{errorContext.Message.MessageId}'. Reason: {discard.Reason}", errorContext.Exception);
                return Task.CompletedTask;
            }

            // When we can't do immediate retries and policy did not honor MaxNumberOfRetries for ImmediateRetries
            if (recoverabilityAction is ImmediateRetry && !immediateRetriesAvailable)
            {
                Logger.Warn("Recoverability policy requested ImmediateRetry however immediate retries are not available with the current endpoint configuration. Moving message to error queue instead.");
                return MoveToError(recoverabilityContext, configuration.Failed.ErrorQueue);
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
                return MoveToError(recoverabilityContext, configuration.Failed.ErrorQueue);
            }

            if (recoverabilityAction is DelayedRetry delayedRetryAction)
            {
                return DeferMessage(delayedRetryAction, recoverabilityContext);
            }

            if (recoverabilityAction is MoveToError moveToError)
            {
                return MoveToError(recoverabilityContext, moveToError.ErrorQueue);
            }

            Logger.Warn("Recoverability policy returned an unsupported recoverability action. Moving message to error queue instead.");
            return MoveToError(recoverabilityContext, configuration.Failed.ErrorQueue);
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
        bool immediateRetriesAvailable;
        bool delayedRetriesAvailable;
        RecoverabilityConfig configuration;

        static ILog Logger = LogManager.GetLogger<RecoverabilityExecutor>();
    }
}