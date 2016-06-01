namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using Faults;
    using JetBrains.Annotations;
    using Logging;
    using Routing;
    using Transports;

    class TimeoutRecoverabilityBehavior
    {
        public TimeoutRecoverabilityBehavior(string errorQueueAddress, string localAddress, IDispatchMessages dispatcher, CriticalError criticalError, TimeoutFailureInfoStorage failureInfoStorage)
        {
            this.localAddress = localAddress;
            this.errorQueueAddress = errorQueueAddress;
            this.dispatcher = dispatcher;
            this.criticalError = criticalError;
            this.failureInfoStorage = failureInfoStorage;
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

                var outgoingMessage = new OutgoingMessage(message.MessageId, message.Headers, message.Body);
                ExceptionHeaderHelper.SetExceptionHeaders(outgoingMessage.Headers, failureInfo.Exception);

                outgoingMessage.Headers[FaultsHeaderKeys.FailedQ] = localAddress;

                var body = new byte[context.BodyStream.Length];
                await context.BodyStream.ReadAsync(body, 0, body.Length).ConfigureAwait(false);

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
        TimeoutFailureInfoStorage failureInfoStorage;
        IDispatchMessages dispatcher;
        string errorQueueAddress;

        string localAddress;

        const int MaxNumberOfFailedRetries = 4;

        static ILog Logger = LogManager.GetLogger<TimeoutRecoverabilityBehavior>();
    }
}