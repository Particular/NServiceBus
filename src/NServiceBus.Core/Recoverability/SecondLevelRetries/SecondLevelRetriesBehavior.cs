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
        public SecondLevelRetriesBehavior(SecondLevelRetryPolicy retryPolicy, string localAddress)
        {
            this.retryPolicy = retryPolicy;
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
                context.Message.Headers.Remove(Headers.Retries);
                throw; // no SLR for poison messages
            }
            catch (Exception ex)
            {
                var message = context.Message;
                var currentRetry = GetNumberOfRetries(message.Headers) + 1;

                TimeSpan delay;

                if (retryPolicy.TryGetDelay(message, ex, currentRetry, out delay))
                {
                    message.RevertToOriginalBodyIfNeeded();
                    var messageToRetry = new OutgoingMessage(message.MessageId, message.Headers, message.Body);

                    messageToRetry.Headers[Headers.Retries] = currentRetry.ToString();
                    messageToRetry.Headers[RetriesTimestamp] = DateTimeExtensions.ToWireFormattedString(DateTime.UtcNow);

                    var dispatchContext = this.CreateRoutingContext(messageToRetry, localAddress, context);

                    context.Extensions.Set(new List<DeliveryConstraint>
                    {
                        new DelayDeliveryWith(delay)
                    });

                    Logger.Warn($"Second Level Retry will reschedule message '{message.MessageId}' after a delay of {delay} because of an exception:", ex);

                    await fork(dispatchContext).ConfigureAwait(false);

                    await context.RaiseNotification(new MessageToBeRetried(currentRetry, delay, context.Message, ex)).ConfigureAwait(false);

                    return;
                }

                message.Headers.Remove(Headers.Retries);
                Logger.WarnFormat("Giving up Second Level Retries for message '{0}'.", message.MessageId);
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

        string localAddress;
        SecondLevelRetryPolicy retryPolicy;

        public const string RetriesTimestamp = "NServiceBus.Retries.Timestamp";

        static ILog Logger = LogManager.GetLogger<SecondLevelRetriesBehavior>();

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