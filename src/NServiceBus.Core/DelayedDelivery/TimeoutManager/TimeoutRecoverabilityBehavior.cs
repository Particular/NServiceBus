namespace NServiceBus
{
    using NServiceBus.Logging;
    using NServiceBus.Pipeline;
    using NServiceBus.Routing;
    using NServiceBus.Transports;
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

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

        bool ShouldAttemptAnotherRetry(ProcessingFailureInfo failureInfo)
        {
            if (failureInfo == null)
            {
                return true;
            }
            return failureInfo.NumberOfFailedAttempts <= MaxNumberOfFailedRetries;
        }

        void HandleProcessingFailure(ITransportReceiveContext context, IncomingMessage message, Exception exception, ProcessingFailureInfo failureInfo)
        {
            failures.RecordFailureInfoForMessage(message.MessageId, exception);

            Logger.Info($"First Level Retry is going to retry message '{message.MessageId}' because of an exception:", exception);

            var numberOfFailedAttempts = failureInfo?.NumberOfFailedAttempts ?? 0;
            notifications.Errors.InvokeMessageHasFailedAFirstLevelRetryAttempt(numberOfFailedAttempts, context.Message, exception);
        }

        void GiveUpForMessage(IncomingMessage message, ProcessingFailureInfo failureInfo)
        {
            notifications.Errors.InvokeMessageHasFailedAFirstLevelRetryAttempt(failureInfo.NumberOfFailedAttempts, message, failureInfo.Exception);

            failures.ClearFailureInfoForMessage(message.MessageId);

            Logger.Info($"Giving up First Level Retries for message '{message.MessageId}'.");
        }

        async Task MoveToErrorQueue(ITransportReceiveContext context, IncomingMessage message, ProcessingFailureInfo failureInfo)
        {
            try
            {
                Logger.Error($"Moving timeout message '{message.MessageId}' to the error queue because processing failed due to an exception:", failureInfo.Exception);

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
        BusNotifications notifications;
        IDispatchMessages dispatcher;
        string errorQueueAddress;
        string localAddress;

        const int MaxNumberOfFailedRetries = 4;

        FailureInfoStorage failures = new FailureInfoStorage();

        static ILog Logger = LogManager.GetLogger<TimeoutRecoverabilityBehavior>();
    }
}