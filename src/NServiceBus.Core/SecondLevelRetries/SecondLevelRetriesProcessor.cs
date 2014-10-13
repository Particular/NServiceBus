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
        public SecondLevelRetriesProcessor(ISendMessages messageSender, IDeferMessages messageDeferrer, FaultManager faultManager, BusNotifications errorCoordinator)
        {
            this.messageSender = messageSender;
            this.messageDeferrer = messageDeferrer;
            this.faultManager = faultManager;
            this.errorCoordinator = errorCoordinator;
            TimeIncrease = TimeSpan.FromSeconds(10);
            NumberOfRetries = 3;
            Disabled = true;
            RetryPolicy = Validate;
        }

        public Func<TransportMessage, TimeSpan> RetryPolicy { get; set; }
        public int NumberOfRetries { get; set; }
        public TimeSpan TimeIncrease { get; set; }
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
            var defer = RetryPolicy.Invoke(message);

            if (defer < TimeSpan.Zero)
            {
                SendToErrorQueue(message);
                return true;
            }

            Defer(defer, message);

            return true;
        }

        void SendToErrorQueue(TransportMessage message)
        {
            logger.ErrorFormat(
                "SLR has failed to resolve the issue with message {0} and will be forwarded to the error queue at {1}",
                message.Id, faultManager.ErrorQueue);

            message.Headers.Remove(Headers.Retries);

            messageSender.Send(message, new SendOptions(faultManager.ErrorQueue));
            errorCoordinator.Errors.InvokeMessageHasBeenSentToErrorQueue(message, new Exception());
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


            messageDeferrer.Defer(message, sendOptions);
        }

        internal TimeSpan Validate(TransportMessage message)
        {
            if (HasReachedMaxTime(message))
            {
                return TimeSpan.MinValue;
            }

            var numberOfRetries = TransportMessageHeaderHelper.GetNumberOfRetries(message);

            var timeToIncreaseInTicks = TimeIncrease.Ticks*(numberOfRetries + 1);
            var timeIncrease = TimeSpan.FromTicks(timeToIncreaseInTicks);

            return numberOfRetries >= NumberOfRetries ? TimeSpan.MinValue : timeIncrease;
        }

        static bool HasReachedMaxTime(TransportMessage message)
        {
            var timestampHeader = TransportMessageHeaderHelper.GetHeader(message, SecondLevelRetriesHeaders.RetriesTimestamp);

            if (String.IsNullOrEmpty(timestampHeader))
            {
                return false;
            }

            try
            {
                var handledAt = DateTimeExtensions.ToUtcDateTime(timestampHeader);

                if (DateTime.UtcNow > handledAt.AddDays(1))
                {
                    return true;
                }
            }
                // ReSharper disable once EmptyGeneralCatchClause
                // this code won't usually throw but in case a user has decided to hack a message/headers and for some bizarre reason 
                // they changed the date and that parse fails, we want to make sure that doesn't prevent the message from being 
                // forwarded to the error queue.
            catch (Exception)
            {
            }

            return false;
        }

        static ILog logger = LogManager.GetLogger<SecondLevelRetriesProcessor>();
        readonly FaultManager faultManager;
        readonly BusNotifications errorCoordinator;
        readonly IDeferMessages messageDeferrer;
        readonly ISendMessages messageSender;
    }
}