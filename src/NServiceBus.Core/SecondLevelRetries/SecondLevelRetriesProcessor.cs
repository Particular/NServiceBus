namespace NServiceBus.SecondLevelRetries
{
    using System;
    using System.Globalization;
    using NServiceBus.Faults;
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
            // ----------------------------------------------------------------------------------
            // This check has now been moved to FaultManager.
            // However the check remains here for backwards compatibility 
            // with messages that could be in the retries queue.
            var defer = SecondLevelRetriesConfiguration.RetryPolicy.Invoke(message);

            if (defer < TimeSpan.Zero)
            {
                SendToErrorQueue(message);
                return true;
            }
            // ----------------------------------------------------------------------------------

            Defer(defer, message);

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

        void Defer(TimeSpan defer, TransportMessage message)
        {
            var retryMessageAt = DateTime.UtcNow + defer;

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

            message.Headers.Remove(FaultsHeaderKeys.TemporatyFailedQueue);
            MessageDeferrer.Defer(message, sendOptions);
        }

        static ILog logger = LogManager.GetLogger<SecondLevelRetriesProcessor>();
    }
}