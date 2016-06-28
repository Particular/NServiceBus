namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using JetBrains.Annotations;
    using Logging;
    using Transports;

    class TimeoutRecoverabilityBehavior
    {
        public TimeoutRecoverabilityBehavior(string errorQueueAddress, string localAddress, CriticalError criticalError, TimeoutFailureInfoStorage failureInfoStorage, RecoveryActionExecutor recoveryActionExecutor)
        {
            this.localAddress = localAddress;
            this.errorQueueAddress = errorQueueAddress;
            this.criticalError = criticalError;
            this.failureInfoStorage = failureInfoStorage;
            this.recoveryActionExecutor = recoveryActionExecutor;
        }

        public async Task Invoke(PushContext context, Func<Task> next)
        {
            var failureInfo = failureInfoStorage.GetFailureInfoForMessage(context.MessageId);

            if (ShouldAttemptAnotherRetry(failureInfo))
            {
                try
                {
                    await next().ConfigureAwait(false);
                    return;
                }
                catch (Exception exception)
                {
                    failureInfoStorage.RecordFailureInfoForMessage(context.MessageId, exception);

                    Logger.Debug($"Going to retry message '{context.MessageId}' from satellite '{localAddress}' because of an exception:", exception);

                    context.ReceiveCancellationTokenSource.Cancel();
                    return;
                }
            }

            failureInfoStorage.ClearFailureInfoForMessage(context.MessageId);

            Logger.Debug($"Giving up Retries for message '{context.MessageId}' from satellite '{localAddress}' after {failureInfo.NumberOfFailedAttempts} attempts.");

            await MoveToErrorQueue(context, failureInfo).ConfigureAwait(false);
        }

        bool ShouldAttemptAnotherRetry([NotNull] TimeoutProcessingFailureInfo failureInfo)
        {
            return failureInfo.NumberOfFailedAttempts <= MaxNumberOfFailedRetries;
        }

        async Task MoveToErrorQueue(PushContext context, TimeoutProcessingFailureInfo failureInfo)
        {
            try
            {
                Logger.Error($"Moving timeout message '{context.MessageId}' from '{localAddress}' to '{errorQueueAddress}' because processing failed due to an exception:", failureInfo.Exception);

                var message = new IncomingMessage(context.MessageId, context.Headers, context.BodyStream);
                await recoveryActionExecutor.MoveToErrorQueue(message, failureInfo.Exception, context.Context).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                criticalError.Raise("Failed to forward failed timeout message to error queue", ex);
                throw;
            }
        }

        CriticalError criticalError;
        TimeoutFailureInfoStorage failureInfoStorage;
        string errorQueueAddress;
        RecoveryActionExecutor recoveryActionExecutor;

        string localAddress;

        const int MaxNumberOfFailedRetries = 4;

        static ILog Logger = LogManager.GetLogger<TimeoutRecoverabilityBehavior>();
    }
}