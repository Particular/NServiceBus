namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using JetBrains.Annotations;
    using Logging;
    using Pipeline;
    using Routing;
    using Transports;

    class TimeoutRecoverabilityBehavior : Behavior<ITransportReceiveContext>
    {
        public TimeoutRecoverabilityBehavior(string errorQueueAddress, string localAddress, IDispatchMessages dispatcher, BusNotifications notifications, CriticalError criticalError)
        {
            this.localAddress = localAddress;
            this.errorQueueAddress = errorQueueAddress;
            this.dispatcher = dispatcher;
            this.notifications = notifications;
            this.criticalError = criticalError;
        }

        public override async Task Invoke(ITransportReceiveContext context, Func<Task> next)
        {
            var message = context.Message;
            var failureInfo = failures.GetFailureInfoForMessage(message.MessageId);

            if (ShouldAttemptAnotherRetry(failureInfo))
            {
                try
                {
                    await next().ConfigureAwait(false);
                    return;
                }
                catch (Exception exception)
                {
                    HandleProcessingFailure(context, message, exception, failureInfo);

                    context.AbortReceiveOperation();
                    return;
                }
            }

            GiveUpForMessage(message, failureInfo);

            await MoveToErrorQueue(context, message, failureInfo).ConfigureAwait(false);
        }

        bool ShouldAttemptAnotherRetry([NotNull] ProcessingFailureInfo failureInfo)
        {
            return failureInfo.NumberOfFailedAttempts <= MaxNumberOfFailedRetries;
        }

        void HandleProcessingFailure(ITransportReceiveContext context, IncomingMessage message, Exception exception, [NotNull] ProcessingFailureInfo failureInfo)
        {
            failures.RecordFailureInfoForMessage(message.MessageId, exception);

            Logger.Debug($"Going to retry message '{message.MessageId}' from satellite '{localAddress}' because of an exception:", exception);

            notifications.Errors.InvokeMessageHasFailedAFirstLevelRetryAttempt(failureInfo.NumberOfFailedAttempts, context.Message, exception);
        }

        void GiveUpForMessage(IncomingMessage message, ProcessingFailureInfo failureInfo)
        {
            notifications.Errors.InvokeMessageHasFailedAFirstLevelRetryAttempt(failureInfo.NumberOfFailedAttempts, message, failureInfo.Exception);

            failures.ClearFailureInfoForMessage(message.MessageId);

            Logger.Debug($"Giving up Retries for message '{message.MessageId}' from satellite '{localAddress}' after {failureInfo.NumberOfFailedAttempts} attempts.");
        }

        async Task MoveToErrorQueue(ITransportReceiveContext context, IncomingMessage message, ProcessingFailureInfo failureInfo)
        {
            try
            {
                Logger.Error($"Moving timeout message '{message.MessageId}' from '{localAddress}' to '{errorQueueAddress}' because processing failed due to an exception:", failureInfo.Exception);

                message.SetExceptionHeaders(failureInfo.Exception, localAddress);

                var outgoingMessage = new OutgoingMessage(message.MessageId, message.Headers, message.Body);
                var routingStrategy = new UnicastRoutingStrategy(errorQueueAddress);
                var addressTag = routingStrategy.Apply(new Dictionary<string, string>());

                var transportOperations = new TransportOperations(new TransportOperation(outgoingMessage, addressTag));

                await dispatcher.Dispatch(transportOperations, context.Extensions).ConfigureAwait(false);

                notifications.Errors.InvokeMessageHasBeenSentToErrorQueue(message, failureInfo.Exception);
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

        FailureInfoStorage failures = new FailureInfoStorage();
        string localAddress;
        BusNotifications notifications;

        const int MaxNumberOfFailedRetries = 4;

        static ILog Logger = LogManager.GetLogger<TimeoutRecoverabilityBehavior>();
    }
}