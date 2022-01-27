namespace NServiceBus
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using NServiceBus.Pipeline;
    using Transport;

    class RecoverabilityPipelineExecutor
    {
        public RecoverabilityPipelineExecutor(
            IServiceProvider serviceProvider,
            IPipelineCache pipelineCache,
            MessageOperations messageOperations,
            Pipeline<IRecoverabilityContext> recoverabilityPipeline)
        {
            this.serviceProvider = serviceProvider;
            this.pipelineCache = pipelineCache;
            this.messageOperations = messageOperations;
            this.recoverabilityPipeline = recoverabilityPipeline;
        }

        public async Task<ErrorHandleResult> Invoke(ErrorContext errorContext, CancellationToken cancellationToken = default)
        {
            using (var childScope = serviceProvider.CreateScope())
            {
                var rootContext = new RootContext(childScope.ServiceProvider, messageOperations, pipelineCache, cancellationToken);
                rootContext.Extensions.Merge(errorContext.Extensions);

                var recoverabilityContext = new RecoverabilityContext(errorContext.Message, rootContext);

                await recoverabilityPipeline.Invoke(recoverabilityContext).ConfigureAwait(false);

                return recoverabilityContext.ActionToTake;
            }

            //var recoveryAction = recoverabilityPolicy(configuration, errorContext);

            //if (recoveryAction is Discard discard)
            //{
            //    Logger.Info($"Discarding message with id '{errorContext.Message.MessageId}'. Reason: {discard.Reason}", errorContext.Exception);
            //    return ErrorHandleResult.Handled;
            //}

            //// When we can't do immediate retries and policy did not honor MaxNumberOfRetries for ImmediateRetries
            //if (recoveryAction is ImmediateRetry && !immediateRetriesAvailable)
            //{
            //    Logger.Warn("Recoverability policy requested ImmediateRetry however immediate retries are not available with the current endpoint configuration. Moving message to error queue instead.");
            //    return await MoveToError(errorContext, configuration.Failed.ErrorQueue, cancellationToken).ConfigureAwait(false);
            //}

            //if (recoveryAction is ImmediateRetry)
            //{
            //    return await RaiseImmediateRetryNotifications(errorContext, cancellationToken).ConfigureAwait(false);
            //}

            //// When we can't do delayed retries, a policy customization probably didn't honor MaxNumberOfRetries for DelayedRetries
            //if (recoveryAction is DelayedRetry && !delayedRetriesAvailable)
            //{
            //    Logger.Warn("Recoverability policy requested DelayedRetry however delayed delivery capability is not available with the current endpoint configuration. Moving message to error queue instead.");
            //    return await MoveToError(errorContext, configuration.Failed.ErrorQueue, cancellationToken).ConfigureAwait(false);
            //}

            //if (recoveryAction is DelayedRetry delayedRetryAction)
            //{
            //    await DeferMessage(delayedRetryAction, errorContext, cancellationToken).ConfigureAwait(false);
            //}

            //if (recoveryAction is MoveToError moveToError)
            //{
            //    return await MoveToError(errorContext, moveToError.ErrorQueue, cancellationToken).ConfigureAwait(false);
            //}

            //Logger.Warn("Recoverability policy returned an unsupported recoverability action. Moving message to error queue instead.");
            //return await MoveToError(errorContext, configuration.Failed.ErrorQueue, cancellationToken).ConfigureAwait(false);
        }

        readonly IServiceProvider serviceProvider;
        readonly IPipelineCache pipelineCache;
        readonly MessageOperations messageOperations;
        readonly Pipeline<IRecoverabilityContext> recoverabilityPipeline;
        //async Task<ErrorHandleResult> RaiseImmediateRetryNotifications(ErrorContext errorContext, CancellationToken cancellationToken)
        //{
        //    Logger.Info($"Immediate Retry is going to retry message '{errorContext.Message.MessageId}' because of an exception:", errorContext.Exception);

        //    if (raiseNotifications)
        //    {
        //        await messageRetryNotification.Raise(
        //                new MessageToBeRetried(
        //                    attempt: errorContext.ImmediateProcessingFailures - 1,
        //                    delay: TimeSpan.Zero,
        //                    immediateRetry: true,
        //                    errorContext: errorContext),
        //                cancellationToken)
        //            .ConfigureAwait(false);
        //    }

        //    return ErrorHandleResult.RetryRequired;
        //}

        //async Task<ErrorHandleResult> MoveToError(ErrorContext errorContext, string errorQueue, CancellationToken cancellationToken)
        //{
        //    var message = errorContext.Message;

        //    Logger.Error($"Moving message '{message.MessageId}' to the error queue '{errorQueue}' because processing failed due to an exception:", errorContext.Exception);

        //    await moveToErrorsExecutor.MoveToErrorQueue(errorQueue, errorContext, cancellationToken).ConfigureAwait(false);

        //    if (raiseNotifications)
        //    {
        //        await messageFaultedNotification.Raise(new MessageFaulted(errorContext, errorQueue), cancellationToken).ConfigureAwait(false);
        //    }

        //    return ErrorHandleResult.Handled;
        //}

        //async Task<ErrorHandleResult> DeferMessage(DelayedRetry action, ErrorContext errorContext, CancellationToken cancellationToken)
        //{
        //    var message = errorContext.Message;

        //    Logger.Warn($"Delayed Retry will reschedule message '{message.MessageId}' after a delay of {action.Delay} because of an exception:", errorContext.Exception);

        //    var currentDelayedRetriesAttempts = await delayedRetryExecutor.Retry(errorContext, action.Delay, cancellationToken).ConfigureAwait(false);

        //    if (raiseNotifications)
        //    {
        //        await messageRetryNotification.Raise(
        //                new MessageToBeRetried(
        //                    attempt: currentDelayedRetriesAttempts,
        //                    delay: action.Delay,
        //                    immediateRetry: false,
        //                    errorContext: errorContext),
        //                cancellationToken)
        //            .ConfigureAwait(false);
        //    }

        //    return ErrorHandleResult.Handled;
        //}

        //readonly INotificationSubscriptions<MessageToBeRetried> messageRetryNotification;
        //readonly INotificationSubscriptions<MessageFaulted> messageFaultedNotification;
        //Func<RecoverabilityConfig, ErrorContext, RecoverabilityAction> recoverabilityPolicy;
        //DelayedRetryExecutor delayedRetryExecutor;
        //MoveToErrorsExecutor moveToErrorsExecutor;
        //bool raiseNotifications;
        //bool immediateRetriesAvailable;
        //bool delayedRetriesAvailable;
        //RecoverabilityConfig configuration;

        //static ILog Logger = LogManager.GetLogger<RecoverabilityPipelineExecutor>();
    }
}