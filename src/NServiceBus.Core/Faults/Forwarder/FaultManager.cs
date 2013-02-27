namespace NServiceBus.Faults.Forwarder
{
    using System;
    using Logging;
    using Transports;
    using Unicast.Queuing;

    /// <summary>
    /// Implementation of IManageMessageFailures by forwarding messages
    /// using ISendMessages.
    /// </summary>
    public class FaultManager : IManageMessageFailures
    {
        void IManageMessageFailures.SerializationFailedForMessage(TransportMessage message, Exception e)
        {
            SendFailureMessage(message, e, true);
        }

        void IManageMessageFailures.ProcessingAlwaysFailsForMessage(TransportMessage message, Exception e)
        {
            SendFailureMessage(message, e);
        }

        void IManageMessageFailures.Init(Address address)
        {
            localAddress = address;
        }

        void SendFailureMessage(TransportMessage message, Exception e, bool serializationException = false)
        {
            SetExceptionHeaders(message, e);

            try
            {
                var destinationQ = RetriesErrorQueue ?? ErrorQueue;
               
                // Intentionally service-locate ISendMessages to avoid circular
                // resolution problem in the container
                var sender = Configure.Instance.Builder.Build<ISendMessages>();

                if (serializationException || MessageWasSentFromSLR(message))
                {
                    sender.Send(message, ErrorQueue);
                    return;
                }

                sender.Send(message, destinationQ);                
            }
            catch (Exception exception)
            {
                var qnfEx = exception as QueueNotFoundException;
                string errorMessage;

                if (qnfEx != null)
                {
                    errorMessage = string.Format("Could not forward failed message to error queue '{0}' as it could not be found.", qnfEx.Queue);
                    Logger.Fatal(errorMessage);
                }
                else
                {
                    errorMessage = "Could not forward failed message to error queue.";
                    Logger.Fatal(errorMessage, exception);
                }

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
            // SecondLevelRetries sat is running and the error happend within that sat.            
            return TransportMessageHelpers.GetReplyToAddress(message) == RetriesErrorQueue;
        }

        void SetExceptionHeaders(TransportMessage message, Exception e)
        {
            message.Headers["NServiceBus.ExceptionInfo.ExceptionType"] = e.GetType().FullName;

            if (e.InnerException != null)
            {
                message.Headers["NServiceBus.ExceptionInfo.InnerExceptionType"] = e.InnerException.GetType().FullName;
            }

            message.Headers["NServiceBus.ExceptionInfo.Message"] = e.Message;
            message.Headers["NServiceBus.ExceptionInfo.Source"] = e.Source;
            message.Headers["NServiceBus.ExceptionInfo.StackTrace"] = e.ToString();
       
            var failedQ = localAddress ?? Address.Local;

            message.Headers[FaultsHeaderKeys.FailedQ] = failedQ.ToString();
            message.Headers["NServiceBus.TimeOfFailure"] = DateTimeExtensions.ToWireFormattedString(DateTime.UtcNow);
        }

        /// <summary>
        /// Endpoint to which message failures are forwarded
        /// </summary>
        public Address ErrorQueue { get; set; }

        /// <summary>
        /// The address of the Second Level Retries input queue when SLR is enabled
        /// </summary>
        public Address RetriesErrorQueue { get; set; }

        Address localAddress;
        static readonly ILog Logger = LogManager.GetLogger(typeof(FaultManager));
    }
}
