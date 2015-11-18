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

        public bool NumberOfRetriesNotExceeded(IncomingMessage message, Exception exception)
        {
            TimeSpan delay;
            var currentRetry = GetNumberOfRetries(message.Headers) + 1;

            return IsEnabled && retryPolicy.TryGetDelay(message, exception, currentRetry, out delay);
        }

        public void LogRetryAttempt(IncomingMessage message, Exception exception)
        {
            //TODO: invoke once and return a slr context from message?
            TimeSpan delay;
            var currentRetry = GetNumberOfRetries(message.Headers) + 1;
            retryPolicy.TryGetDelay(message, exception, currentRetry, out delay);

            Logger.Warn($"Second Level Retry will reschedule message '{message.MessageId}' after a delay of {delay} because of an exception:", exception);

            notifications.Errors.InvokeMessageHasBeenSentToSecondLevelRetries(currentRetry, message, exception);
        }

        public async Task<bool> TryHandle(TransportReceiveContext context, Exception exception)
        {
            TimeSpan delay;
            var currentRetry = GetNumberOfRetries(context.Message.Headers) + 1;

            if (IsEnabled && retryPolicy.TryGetDelay(context.Message, exception, currentRetry, out delay))
            {
                await QueueForDelayedDelivery(context, currentRetry, delay, exception).ConfigureAwait(false);

                return true;
            }

            context.Message.Headers.Remove(Headers.Retries);
            Logger.WarnFormat("Giving up Second Level Retries for message '{0}'.", context.Message.MessageId);

            return false;
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

        private bool IsEnabled => isEnabled && !Suppressed;

        public bool Suppressed { get; set; }


        public async Task MoveToTimeoutQueue(TransportReceiveContext context, Exception exception)
        {
            var message = context.Message;

            TimeSpan delay;
            var currentRetry = GetNumberOfRetries(message.Headers) + 1;
            retryPolicy.TryGetDelay(message, exception, currentRetry, out delay);

            message.RevertToOriginalBodyIfNeeded();

            var messageToRetry = new OutgoingMessage(message.MessageId, message.Headers, message.Body);

            messageToRetry.Headers[Headers.Retries] = currentRetry.ToString();
            messageToRetry.Headers[RetriesTimestamp] = DateTimeExtensions.ToWireFormattedString(DateTime.UtcNow);

            var dispatchContext = new RoutingContext(messageToRetry, new UnicastRoutingStrategy(localAddress), context);

            context.Set(new List<DeliveryConstraint>
            {
                new DelayDeliveryWith(delay)
            });

            await dispatchPipeline.Invoke(dispatchContext).ConfigureAwait(false);
        }

        public void GiveUpForMessage(IncomingMessage message)
        {
            message.Headers.Remove(Headers.Retries);

            Logger.WarnFormat("Giving up Second Level Retries for message '{0}'.", message.MessageId);
        }

        private async Task QueueForDelayedDelivery(TransportReceiveContext context, int currentRetry, TimeSpan delay, Exception exception)
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
        bool isEnabled;

        static ILog Logger = LogManager.GetLogger<SecondLevelRetriesHandler>();

        public const string RetriesTimestamp = "NServiceBus.Retries.Timestamp";
    }
}