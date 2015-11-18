namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using DelayedDelivery;
    using DeliveryConstraints;
    using Logging;
    using Pipeline;
    using Pipeline.Contexts;
    using Recoverability.SecondLevelRetries;
    using Routing;
    using TransportDispatch;
    using Transports;

    class SecondLevelRetriesHandler
    {
        public SecondLevelRetriesHandler(
            IPipelineBase<RoutingContext> dispatchPipeline, 
            SecondLevelRetryPolicy retryPolicy, 
            BusNotifications notifications, 
            string localAddress, 
            bool isEnabled = true)
        {
            this.dispatchPipeline = dispatchPipeline;
            this.retryPolicy = retryPolicy;
            this.notifications = notifications;
            this.localAddress = localAddress;
            this.isEnabled = isEnabled;
        }

        public bool ShouldPerformSlr(IncomingMessage message, Exception exception,  out TimeSpan delay, out int currentRetry)
        {
            currentRetry = GetNumberOfRetries(message.Headers) + 1;

            return retryPolicy.TryGetDelay(message, exception, currentRetry, out delay);
        }

        static int GetNumberOfRetries(Dictionary<string, string> headers)
        {
            string value;
            if (headers.TryGetValue(Headers.Retries, out value))
            {
                int i;
                if (int.TryParse(value, out i))
                {
                    return i;
                }
            }
            return 0;
        }

        public bool IsEnabled => isEnabled;

        public async Task QueueForDelayedDelivery(TransportReceiveContext context, int currentRetry, TimeSpan delay, Exception exception)
        {
            var message = context.Message;

            message.RevertToOriginalBodyIfNeeded();

            var messageToRetry = new OutgoingMessage(message.MessageId, message.Headers, message.Body);

            messageToRetry.Headers[Headers.Retries] = currentRetry.ToString();
            messageToRetry.Headers[RetriesTimestamp] = DateTimeExtensions.ToWireFormattedString(DateTime.UtcNow);

            var dispatchContext = new RoutingContext(messageToRetry, new UnicastRoutingStrategy(localAddress), context);

            context.Set(new List<DeliveryConstraint>
            {
                new DelayDeliveryWith(delay)
            });

            Logger.Warn($"Second Level Retry will reschedule message '{message.MessageId}' after a delay of {delay} because of an exception:", exception);

            await dispatchPipeline.Invoke(dispatchContext).ConfigureAwait(false);

            notifications.Errors.InvokeMessageHasBeenSentToSecondLevelRetries(currentRetry, message, exception);
        }

        IPipelineBase<RoutingContext> dispatchPipeline;
        SecondLevelRetryPolicy retryPolicy;
        BusNotifications notifications;
        string localAddress;

        static ILog Logger = LogManager.GetLogger<SecondLevelRetriesHandler>();
        bool isEnabled;

        public const string RetriesTimestamp = "NServiceBus.Retries.Timestamp";
    }
}