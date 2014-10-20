namespace NServiceBus.Faults.Forwarder
{
    using System;
    using NServiceBus.Logging;
    using NServiceBus.ObjectBuilder;
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
        public FaultManager(IBuilder builder, Configure config, BusNotifications busNotifications)
        {
            this.builder = builder;
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
            var sender = LocateMessageSender();
            sender.Send(message, new SendOptions(ErrorQueue));
        }

        // Intentionally service-locate ISendMessages to avoid circular
        // resolution problem in the container
        ISendMessages LocateMessageSender()
        {
            return builder.Build<ISendMessages>();
        }

        void HandleProcessingAlwaysFailsForMessage(TransportMessage message, Exception e, int numberOfRetries)
        {
            message.SetExceptionHeaders(e, localAddress ?? config.LocalAddress);

            var destinationQ = RetriesErrorQueue ?? ErrorQueue;
            var sender = LocateMessageSender();
            var failedMessageException = new FailedMessage.FailedMessageException(e.GetType().FullName, e.GetMessage(), e.Source, e.ToString());
            
            if (MessageWasSentFromSLR(message))
            {
                sender.Send(message, new SendOptions(ErrorQueue));
                busNotifications.Errors.InvokeMessageHasBeenSentToErrorQueue(message, failedMessageException);
                return;
            }

            sender.Send(message, new SendOptions(destinationQ));

            var flrPart = numberOfRetries > 0
                ? string.Format("Message with '{0}' id has failed FLR and", message.Id)
                : string.Format("FLR is disabled and the message '{0}'", message.Id);

            //HACK: We need this hack here till we refactor the SLR to be a first class concept in the TransportReceiver
            if (RetriesErrorQueue == null)
            {
                Logger.ErrorFormat("{0} will be moved to the configured error queue.", flrPart);
                busNotifications.Errors.InvokeMessageHasBeenSentToErrorQueue(message, failedMessageException);
            }
            else
            {
                var retryAttempt = TransportMessageHeaderHelper.GetNumberOfRetries(message) + 1;

                Logger.WarnFormat("{0} will be handed over to SLR for retry attempt {1}.", flrPart, retryAttempt);
                busNotifications.Errors.InvokeMessageHasBeenSentToSecondLevelRetries(retryAttempt, message, e);
            }
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
        readonly IBuilder builder;
        readonly Configure config;
        readonly BusNotifications busNotifications;
        Address localAddress;
    }
}