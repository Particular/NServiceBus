namespace NServiceBus.Faults.Forwarder
{
    using System;
    using NServiceBus.Logging;
    using NServiceBus.SecondLevelRetries;
    using NServiceBus.SecondLevelRetries.Helpers;
    using NServiceBus.Transports;
    using NServiceBus.Unicast;
    using NServiceBus.Unicast.Queuing;

    /// <summary>
    ///     Implementation of IManageMessageFailures by forwarding messages
    ///     using ISendMessages.
    /// </summary>
    class FaultManager : IManageMessageFailures
    {
        public FaultManager(ISendMessages sender, Configure config, BusNotifications busNotifications)
        {
            this.sender = sender;
            this.config = config;
            this.busNotifications = busNotifications;
        }

        /// <summary>
        ///     Endpoint to which message failures are forwarded
        /// </summary>
        public Address ErrorQueue { get; set; }

        /// <summary>
        ///     The address of the Second Level Retries input queue when SLR is enabled
        /// </summary>
        public Address RetriesQueue { get; set; }

        public SecondLevelRetriesConfiguration SecondLevelRetriesConfiguration { get; set; }

        void IManageMessageFailures.SerializationFailedForMessage(TransportMessage message, Exception e)
        {
            TryHandleFailure(() => HandleSerializationFailedForMessage(message, e));
        }

        void IManageMessageFailures.ProcessingAlwaysFailsForMessage(TransportMessage message, Exception e)
        {
            TryHandleFailure(() => HandleProcessingAlwaysFailsForMessage(message, e, GetNumberOfFirstLevelRetries(message)));
        }

        void IManageMessageFailures.Init(Address address)
        {
            localAddress = address;
        }

        void HandleSerializationFailedForMessage(TransportMessage message, Exception e)
        {
            message.SetExceptionHeaders(e, localAddress ?? config.LocalAddress);
            SendToErrorQueue(message, e);
        }

        void HandleProcessingAlwaysFailsForMessage(TransportMessage message, Exception e, int numberOfRetries)
        {
            message.SetExceptionHeaders(e, localAddress ?? config.LocalAddress);

            if (MessageWasSentFromSLR(message))
            {
                SendToErrorQueue(message, e);
                return;
            }

            var flrPart = numberOfRetries > 0
                ? $"Message with '{message.Id}' id has failed FLR and"
                : $"FLR is disabled and the message '{message.Id}'";

            //HACK: We need this hack here till we refactor the SLR to be a first class concept in the TransportReceiver
            if (RetriesQueue == null)
            {
                Logger.ErrorFormat("{0} will be moved to the configured error queue.", flrPart);
                SendToErrorQueue(message, e);
                return;
            }

            var defer = SecondLevelRetriesConfiguration.RetryPolicy.Invoke(message);

            if (defer < TimeSpan.Zero)
            {
                Logger.ErrorFormat(
                    "SLR has failed to resolve the issue with message {0} and will be forwarded to the error queue at {1}",
                    message.Id, ErrorQueue);
                SendToErrorQueue(message, e);
                return;
            }

            SendToRetriesQueue(message, e, defer, flrPart);
        }

        void SendToErrorQueue(TransportMessage message, Exception exception)
        {
            message.TimeToBeReceived = TimeSpan.MaxValue;

            if (message.Headers.ContainsKey(Headers.FLRetries))
            {
                message.Headers.Remove(Headers.FLRetries);
            }

            if (message.Headers.ContainsKey(Headers.Retries))
            {
                message.Headers.Remove(Headers.Retries);
            }

            sender.Send(message, new SendOptions(ErrorQueue));
            busNotifications.Errors.InvokeMessageHasBeenSentToErrorQueue(message, exception);
        }

        void SendToRetriesQueue(TransportMessage message, Exception e, TimeSpan defer, string flrPart)
        {
            message.TimeToBeReceived = TimeSpan.MaxValue;

            DateTime retryMessageAt;

            try
            {
                retryMessageAt = DateTime.UtcNow + defer;
            }
            catch (ArgumentOutOfRangeException)
            {
                Logger.WarnFormat("SLR RetryPolicy TimeSpan is too large: {0}. Retry will occur at DateTime.MaxValue",defer);
                retryMessageAt = DateTime.MaxValue;
            }

            TransportMessageHeaderHelper.SetHeader(message, SecondLevelRetriesHeaders.RetriesRetryAt,
                DateTimeExtensions.ToWireFormattedString(retryMessageAt));

            sender.Send(message, new SendOptions(RetriesQueue));

            var retryAttempt = TransportMessageHeaderHelper.GetNumberOfRetries(message) + 1;

            Logger.WarnFormat("{0} will be handed over to SLR for retry attempt {1}.", flrPart, retryAttempt);
            busNotifications.Errors.InvokeMessageHasBeenSentToSecondLevelRetries(retryAttempt, message, e);
        }

        void TryHandleFailure(Action failureHandlingAction)
        {
            try
            {
                failureHandlingAction();
            }
            catch (QueueNotFoundException exception)
            {
                var errorMessage = $"Could not forward failed message to error queue '{exception.Queue}' as it could not be found.";
                Logger.Fatal(errorMessage);
                throw new InvalidOperationException(errorMessage, exception);
            }
            catch (Exception exception)
            {
                var errorMessage = "Could not forward failed message to error queue.";
                Logger.Fatal(errorMessage, exception);
                throw new InvalidOperationException(errorMessage, exception);
            }
        }

        bool MessageWasSentFromSLR(TransportMessage message)
        {
            if (RetriesQueue == null)
            {
                return false;
            }

            // if the reply to address == ErrorQueue and RealErrorQueue is not null, the
            // SecondLevelRetries sat is running and the error happened within that sat.
            return TransportMessageHeaderHelper.GetAddressOfFaultingEndpoint(message) == RetriesQueue;
        }

        static int GetNumberOfFirstLevelRetries(TransportMessage message)
        {
            string value;
            if (message.Headers.TryGetValue(Headers.FLRetries, out value))
            {
                int i;
                if (int.TryParse(value, out i))
                {
                    return i;
                }
            }
            return 0;
        }

        static ILog Logger = LogManager.GetLogger<FaultManager>();
        readonly BusNotifications busNotifications;
        readonly Configure config;
        readonly ISendMessages sender;
        Address localAddress;
    }
}