namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using DelayedDelivery;
    using DeliveryConstraints;
    using Logging;
    using Pipeline;
    using Transports;

    class SecondLevelRetriesBehavior : ForkConnector<ITransportReceiveContext, IRoutingContext>
    {
        public SecondLevelRetriesBehavior(SecondLevelRetryPolicy retryPolicy, BusNotifications notifications, string localAddress)
        {
            this.retryPolicy = retryPolicy;
            this.notifications = notifications;
            this.localAddress = localAddress;
        }

        public override async Task Invoke(ITransportReceiveContext context, Func<Task> next, Func<IRoutingContext, Task> fork)
        {
            try
            {
                await next().ConfigureAwait(false);
            }
            catch (MessageDeserializationException)
            {
                context.Headers.Remove(Headers.Retries);
                throw; // no SLR for poison messages
            }
            catch (Exception ex)
            {
                var currentRetry = GetNumberOfRetries(context.Headers) + 1;

                TimeSpan delay;

                if (retryPolicy.TryGetDelay(context, ex, currentRetry, out delay))
                {
                    context.RevertToOriginalBodyIfNeeded();
                    var messageToRetry = new OutgoingMessage(context.MessageId, context.Headers, context.Body);

                    messageToRetry.Headers[Headers.Retries] = currentRetry.ToString();
                    messageToRetry.Headers[RetriesTimestamp] = DateTimeExtensions.ToWireFormattedString(DateTime.UtcNow);

                    var dispatchContext = this.CreateRoutingContext(messageToRetry, localAddress, context);

                    context.Extensions.Set(new List<DeliveryConstraint>
                    {
                        new DelayDeliveryWith(delay)
                    });

                    Logger.Warn($"Second Level Retry will reschedule message '{context.MessageId}' after a delay of {delay} because of an exception:", ex);

                    await fork(dispatchContext).ConfigureAwait(false);

                    notifications.Errors.InvokeMessageHasBeenSentToSecondLevelRetries(currentRetry, context, ex);

                    return;
                }

                context.Headers.Remove(Headers.Retries);
                Logger.WarnFormat("Giving up Second Level Retries for message '{0}'.", context.MessageId);
                throw;
            }

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

        SecondLevelRetryPolicy retryPolicy;
        BusNotifications notifications;
        string localAddress;

        static ILog Logger = LogManager.GetLogger<SecondLevelRetriesBehavior>();

        public const string RetriesTimestamp = "NServiceBus.Retries.Timestamp";

        public class Registration : RegisterStep
        {
            public Registration()
                : base("SecondLevelRetries", typeof(SecondLevelRetriesBehavior), "Performs second level retries")
            {
                InsertBeforeIfExists("FirstLevelRetries");
            }
        }

    }
}