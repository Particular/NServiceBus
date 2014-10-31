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
        public Address RetriesErrorQueue { get; set; }

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
            sender.Send(message, new SendOptions(ErrorQueue));
        }

        void HandleProcessingAlwaysFailsForMessage(TransportMessage message, Exception e, int numberOfRetries)
        {
            message.SetExceptionHeaders(e, localAddress ?? config.LocalAddress);

            if (MessageWasSentFromSLR(message))
            {
                sender.Send(message, new SendOptions(ErrorQueue));
                busNotifications.Errors.InvokeMessageHasBeenSentToErrorQueue(message, e);
                return;
            }

            var flrPart = numberOfRetries > 0
                ? string.Format("Message with '{0}' id has failed FLR and", message.Id)
                : string.Format("FLR is disabled and the message '{0}'", message.Id);

            //HACK: We need this hack here till we refactor the SLR to be a first class concept in the TransportReceiver
            if (RetriesErrorQueue == null)
            {
                sender.Send(message, new SendOptions(ErrorQueue));
                Logger.ErrorFormat("{0} will be moved to the configured error queue.", flrPart);
                busNotifications.Errors.InvokeMessageHasBeenSentToErrorQueue(message, e);
                return;
            }

            var defer = SecondLevelRetriesConfiguration.RetryPolicy.Invoke(message);

            if (defer < TimeSpan.Zero)
            {
                SendToErrorQueue(message, e);
                return;
            }
            sender.Send(message, new SendOptions(RetriesErrorQueue));

            var retryAttempt = TransportMessageHeaderHelper.GetNumberOfRetries(message) + 1;

            Logger.WarnFormat("{0} will be handed over to SLR for retry attempt {1}.", flrPart, retryAttempt);
            busNotifications.Errors.InvokeMessageHasBeenSentToSecondLevelRetries(retryAttempt, message, e);
        }

        void SendToErrorQueue(TransportMessage message, Exception exception)
        {
            Logger.ErrorFormat(
                "SLR has failed to resolve the issue with message {0} and will be forwarded to the error queue at {1}",
                message.Id, ErrorQueue);

            message.Headers.Remove(Headers.Retries);

            sender.Send(message, new SendOptions(ErrorQueue));
            busNotifications.Errors.InvokeMessageHasBeenSentToErrorQueue(message, exception);
        }

        void TryHandleFailure(Action failureHandlingAction)
        {
            try
            {
                failureHandlingAction();
            }
            catch (QueueNotFoundException exception)
            {
                var errorMessage = string.Format("Could not forward failed message to error queue '{0}' as it could not be found.", exception.Queue);
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
            if (RetriesErrorQueue == null)
            {
                return false;
            }

            // if the reply to address == ErrorQueue and RealErrorQueue is not null, the
            // SecondLevelRetries sat is running and the error happened within that sat.            
            return TransportMessageHeaderHelper.GetAddressOfFaultingEndpoint(message) == RetriesErrorQueue;
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