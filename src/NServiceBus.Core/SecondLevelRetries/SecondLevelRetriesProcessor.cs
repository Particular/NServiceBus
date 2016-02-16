namespace NServiceBus.SecondLevelRetries
{
    using System;
    using System.Globalization;
    using NServiceBus.Faults.Forwarder;
    using NServiceBus.Logging;
    using NServiceBus.Satellites;
    using NServiceBus.SecondLevelRetries.Helpers;
    using NServiceBus.Transports;
    using NServiceBus.Unicast;

    class SecondLevelRetriesProcessor : ISatellite
    {
        public SecondLevelRetriesProcessor()
        {
            Disabled = true;
        }

        public SecondLevelRetriesConfiguration SecondLevelRetriesConfiguration { get; set; }
        public ISendMessages MessageSender { get; set; }
        public IDeferMessages MessageDeferrer { get; set; }
        public FaultManager FaultManager { get; set; }
        public Address InputAddress { get; set; }
        public bool Disabled { get; set; }

        public void Start()
        {
        }

        public void Stop()
        {
        }

        public bool Handle(TransportMessage message)
        {
            DateTime retryMessageAt;

            if (TransportMessageHeaderHelper.HeaderExists(message, SecondLevelRetriesHeaders.RetriesRetryAt))
            {
                retryMessageAt = DateTimeExtensions.ToUtcDateTime(TransportMessageHeaderHelper.GetHeader(message, SecondLevelRetriesHeaders.RetriesRetryAt));
            }
            else
            {
                var defer = SecondLevelRetriesConfiguration.RetryPolicy.Invoke(message);

                if (defer < TimeSpan.Zero)
                {
                    SendToErrorQueue(message);
                    return true;
                }

                retryMessageAt = DateTime.UtcNow + defer;
            }

            if (retryMessageAt <= DateTime.UtcNow)
            {
                ReturnToFaultingEndpoint(message);
            }

            Defer(retryMessageAt, message);

            return true;
        }

        void SendToErrorQueue(TransportMessage message)
        {
            logger.ErrorFormat(
                "SLR has failed to resolve the issue with message {0} and will be forwarded to the error queue at {1}",
                message.Id, FaultManager.ErrorQueue);

            message.Headers.Remove(Headers.Retries);

            MessageSender.Send(message, new SendOptions(FaultManager.ErrorQueue));
        }

        void ReturnToFaultingEndpoint(TransportMessage message)
        {
            var addressOfFaultingEndpoint = TransportMessageHeaderHelper.GetAddressOfFaultingEndpoint(message);

            if (message.Headers.ContainsKey(SecondLevelRetriesHeaders.RetriesRetryAt))
            {
                message.Headers.Remove(SecondLevelRetriesHeaders.RetriesRetryAt);
            }

            MessageSender.Send(message, new SendOptions(addressOfFaultingEndpoint));
        }

        void Defer(DateTime retryMessageAt, TransportMessage message)
        {
            TransportMessageHeaderHelper.SetHeader(message, Headers.Retries,
                (TransportMessageHeaderHelper.GetNumberOfRetries(message) + 1).ToString(CultureInfo.InvariantCulture));

            var addressOfFaultingEndpoint = TransportMessageHeaderHelper.GetAddressOfFaultingEndpoint(message);

            if (!TransportMessageHeaderHelper.HeaderExists(message, SecondLevelRetriesHeaders.RetriesTimestamp))
            {
                TransportMessageHeaderHelper.SetHeader(message, SecondLevelRetriesHeaders.RetriesTimestamp,
                    DateTimeExtensions.ToWireFormattedString(DateTime.UtcNow));
            }

            logger.DebugFormat("Defer message and send it to {0}", addressOfFaultingEndpoint);

            var sendOptions = new SendOptions(addressOfFaultingEndpoint)
            {
                DeliverAt = retryMessageAt
            };

            MessageDeferrer.Defer(message, sendOptions);
        }

        static ILog logger = LogManager.GetLogger<SecondLevelRetriesProcessor>();
    }
}