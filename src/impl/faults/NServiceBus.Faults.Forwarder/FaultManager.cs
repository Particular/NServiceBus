
namespace NServiceBus.Faults.Forwarder
{
	using System;
	using NServiceBus.Unicast.Transport;
    using Unicast.Queuing;

    /// <summary>
    /// Implementation of IManageMessageFailures by forwarding messages
    /// using ISendMessages.
    /// </summary>
    public class FaultManager : IManageMessageFailures
    {
        void IManageMessageFailures.SerializationFailedForMessage(TransportMessage message, Exception e)
        {
            this.SendFailureMessage(message, e, "SerializationFailed");
        }

        void IManageMessageFailures.ProcessingAlwaysFailsForMessage(TransportMessage message, Exception e)
        {
            if (SanitizeProcessingExceptions)
                e = ExceptionSanitizer.Sanitize(e);

            this.SendFailureMessage(message, e, "ProcessingFailed");
        }

        // Intentionally service-locate ISendMessages to avoid circular
        // resolution problem in the container
        private void SendFailureMessage(TransportMessage message, Exception e, string reason)
        {
            SetExceptionHeaders(message, e, reason);
            var sender = Configure.Instance.Builder.Build<ISendMessages>();
            sender.Send(message, this.ErrorQueue);
        }

        private static void SetExceptionHeaders(TransportMessage message, Exception e, string reason)
        {
            message.Headers["ExceptionInfo.Reason"] = reason;
			message.Headers["ExceptionInfo.ExceptionType"] = e.GetType().FullName;

			if (e.InnerException != null)
				message.Headers["ExceptionInfo.InnerExceptionType"] = e.InnerException.GetType().FullName;
            
			message.Headers["ExceptionInfo.HelpLink"] = e.HelpLink;
            message.Headers["ExceptionInfo.Message"] = e.Message;
            message.Headers["ExceptionInfo.Source"] = e.Source;
            message.Headers["ExceptionInfo.StackTrace"] = e.StackTrace;
        }

        /// <summary>
        /// Endpoint to which message failures are forwarded
        /// </summary>
        public string ErrorQueue { get; set; }

        /// <summary>
        /// Indicates of exceptions should be sanitized before sending them on
        /// </summary>
        public bool SanitizeProcessingExceptions { get; set; }
    }
}
