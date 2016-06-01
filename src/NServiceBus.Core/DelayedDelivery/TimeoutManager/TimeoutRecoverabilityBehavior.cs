namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using JetBrains.Annotations;
    using Logging;
    using Routing;
    using Transports;

    class TimeoutRecoverabilityBehavior
    {
        public TimeoutRecoverabilityBehavior(string errorQueueAddress, string localAddress, IDispatchMessages dispatcher, CriticalError criticalError)
        {
            this.localAddress = localAddress;
            this.errorQueueAddress = errorQueueAddress;
            this.dispatcher = dispatcher;
            this.criticalError = criticalError;
        }

        public async Task Invoke(PushContext context, Func<Task> next)
        {
            var failureInfo = failures.GetFailureInfoForMessage(context.MessageId);

            if (ShouldAttemptAnotherRetry(failureInfo))
            {
                try
                {
                    await next().ConfigureAwait(false);
                    return;
                }
                catch (Exception exception)
                {
                    failures.RecordFailureInfoForMessage(context.MessageId, exception);

                    Logger.Debug($"Going to retry message '{context.MessageId}' from satellite '{localAddress}' because of an exception:", exception);

                    context.ReceiveCancellationTokenSource.Cancel();
                    return;
                }
            }

            failures.ClearFailureInfoForMessage(context.MessageId);

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

                ExceptionHeaderHelper.SetExceptionHeaders(context.Headers, failureInfo.Exception, localAddress, null);

                var body = new byte[context.BodyStream.Length];
                context.BodyStream.Read(body, 0, body.Length);

                var outgoingMessage = new OutgoingMessage(context.MessageId, context.Headers, body);
                var addressTag = new UnicastAddressTag(errorQueueAddress);

                var transportOperations = new TransportOperations(new TransportOperation(outgoingMessage, addressTag));

                await dispatcher.Dispatch(transportOperations, context.Context).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                criticalError.Raise("Failed to forward failed timeout message to error queue", ex);
                throw;
            }
        }

        CriticalError criticalError;
        IDispatchMessages dispatcher;
        string errorQueueAddress;

        TimeoutFailureInfoStorage failures = new TimeoutFailureInfoStorage();
        string localAddress;

        const int MaxNumberOfFailedRetries = 4;

        static ILog Logger = LogManager.GetLogger<TimeoutRecoverabilityBehavior>();
    }
}