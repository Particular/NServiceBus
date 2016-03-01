namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using JetBrains.Annotations;
    using NServiceBus.Logging;
    using NServiceBus.Pipeline;
    using NServiceBus.Routing;
    using NServiceBus.Transports;

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
                    HandleProcessingFailure(context, exception, failureInfo);

                    context.AbortReceiveOperation();
                    return;
                }
            }

            GiveUpForMessage(context, failureInfo);

            await MoveToErrorQueue(context, failureInfo).ConfigureAwait(false);
        }

        bool ShouldAttemptAnotherRetry([NotNull] ProcessingFailureInfo failureInfo)
        {
            return failureInfo.NumberOfFailedAttempts <= MaxNumberOfFailedRetries;
        }

        void HandleProcessingFailure(ITransportReceiveContext context, Exception exception, [NotNull] ProcessingFailureInfo failureInfo)
        {
            failures.RecordFailureInfoForMessage(context.MessageId, exception);

            Logger.Debug($"Going to retry message '{context.MessageId}' from satellite '{localAddress}' because of an exception:", exception);

            notifications.Errors.InvokeMessageHasFailedAFirstLevelRetryAttempt(failureInfo.NumberOfFailedAttempts, context, exception);
        }

        void GiveUpForMessage(ITransportReceiveContext context, ProcessingFailureInfo failureInfo)
        {
            notifications.Errors.InvokeMessageHasFailedAFirstLevelRetryAttempt(failureInfo.NumberOfFailedAttempts, context, failureInfo.Exception);

            failures.ClearFailureInfoForMessage(context.MessageId);

            Logger.Debug($"Giving up Retries for message '{context.MessageId}' from satellite '{localAddress}' after {failureInfo.NumberOfFailedAttempts} attempts.");
        }

        async Task MoveToErrorQueue(ITransportReceiveContext context, ProcessingFailureInfo failureInfo)
        {
            try
            {
                Logger.Error($"Moving timeout message '{context.MessageId}' from '{localAddress}' to '{errorQueueAddress}' because processing failed due to an exception:", failureInfo.Exception);

                context.SetExceptionHeaders(failureInfo.Exception, localAddress);

                var outgoingMessage = new OutgoingMessage(context.MessageId, context.Headers, context.Body);
                var routingStrategy = new UnicastRoutingStrategy(errorQueueAddress);
                var addressTag = routingStrategy.Apply(new Dictionary<string, string>());

                var transportOperations = new TransportOperations(new TransportOperation(outgoingMessage, addressTag));

                await dispatcher.Dispatch(transportOperations, context.Extensions).ConfigureAwait(false);

                notifications.Errors.InvokeMessageHasBeenSentToErrorQueue(context, failureInfo.Exception);
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