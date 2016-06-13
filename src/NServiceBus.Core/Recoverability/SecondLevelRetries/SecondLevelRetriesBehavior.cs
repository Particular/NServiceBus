﻿namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using Pipeline;
    using Transports;

    class SecondLevelRetriesBehavior
    {
        public SecondLevelRetriesBehavior(SecondLevelRetryPolicy retryPolicy, string localAddress, FailureInfoStorage failureInfoStorage, IDispatchMessages dispatcher)
        {
            this.retryPolicy = retryPolicy;
         }

        public bool Invoke(Exception exception, int numberOfSecondLevelAttempts, Dictionary<string,string> headers)
        {
            if (exception is MessageDeserializationException)
            {
                headers.Remove(Headers.Retries); //???
                return false;
            }

            TimeSpan delay;

            if (!retryPolicy.TryGetDelay(headers, exception, numberOfSecondLevelAttempts, out delay))
            {
                return false;
            }

            headers[Headers.Retries] = numberOfSecondLevelAttempts.ToString();
            headers[Headers.RetriesTimestamp] = DateTimeExtensions.ToWireFormattedString(DateTime.UtcNow);

            //var operation = new TransportOperation(
            //    new OutgoingMessage(failedMessage.MessageId, failedMessage.Headers, failedMessage.Body),
            //    new UnicastAddressTag(localAddress));

            ////Question: is this proper way to do it
            //context.Set(new List<DeliveryConstraint>
            //{
            //    new DelayDeliveryWith(delay)
            //});

            //Logger.Warn($"Second Level Retry will reschedule message '{failedMessage.MessageId}' after a delay of {delay} because of an exception:", exception);

            ////Question: this is wrong we should set the destination address to timeout manager storage queue
            //await dispatcher.Dispatch(new TransportOperations(operation), context).ConfigureAwait(false);

            return true;
        }

        SecondLevelRetryPolicy retryPolicy;

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