namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using DelayedDelivery;
    using DeliveryConstraints;
    using Extensibility;
    using Logging;
    using Pipeline;
    using Routing;
    using Transports;

    class SecondLevelRetriesBehavior
    {
        public SecondLevelRetriesBehavior(SecondLevelRetryPolicy retryPolicy, string localAddress, FailureInfoStorage failureInfoStorage, IDispatchMessages dispatcher)
        {
            this.retryPolicy = retryPolicy;
            this.localAddress = localAddress;
            this.failureInfoStorage = failureInfoStorage;
            this.dispatcher = dispatcher;
        }

        public async Task<bool> Invoke(Exception exception, int numberOfSecondLevelAttempts, IncomingMessage failedMessage, ContextBag context)
        {
            if (exception is MessageDeserializationException)
            {
                failedMessage.Headers.Remove(Headers.Retries); //???
                return false;
            }

            TimeSpan delay;

            if (!retryPolicy.TryGetDelay(failedMessage, exception, numberOfSecondLevelAttempts, out delay))
            {
                return false;
            }

            failedMessage.Headers[Headers.Retries] = numberOfSecondLevelAttempts.ToString();
            failedMessage.Headers[Headers.RetriesTimestamp] = DateTimeExtensions.ToWireFormattedString(DateTime.UtcNow);

            var operation = new TransportOperation(
                new OutgoingMessage(failedMessage.MessageId, failedMessage.Headers, failedMessage.Body), 
                new UnicastAddressTag(localAddress));
            
            //Question: is this proper way to do it
            context.Set(new List<DeliveryConstraint>
            {
                new DelayDeliveryWith(delay)
            });

            Logger.Warn($"Second Level Retry will reschedule message '{failedMessage.MessageId}' after a delay of {delay} because of an exception:", exception);

            //Question: this is wrong we should set the destination address to timeout manager storage queue
            await dispatcher.Dispatch(new TransportOperations(operation), context).ConfigureAwait(false);

            return true;
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

        FailureInfoStorage failureInfoStorage;
        readonly IDispatchMessages dispatcher;
        string localAddress;
        SecondLevelRetryPolicy retryPolicy;

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