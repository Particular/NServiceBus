
namespace NServiceBus.Faults.Forwarder
{
	using System;
	using Unicast.Transport;
    using Unicast.Queuing;

    /// <summary>
    /// Implementation of IManageMessageFailures by forwarding messages
    /// using ISendMessages.
    /// </summary>
    public class FaultManager : IManageMessageFailures
    {
        void IManageMessageFailures.SerializationFailedForMessage(TransportMessage message, Exception e)
        {
            SendFailureMessage(message, e, "SerializationFailed");
        }

        void IManageMessageFailures.ProcessingAlwaysFailsForMessage(TransportMessage message, Exception e)
        {
            if (SanitizeProcessingExceptions)
                e = ExceptionSanitizer.Sanitize(e);

            var id = message.Id;
            SendFailureMessage(message, e, "ProcessingFailed"); //overwrites message.Id
            message.Id = id;
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
            message.Headers["NServiceBus.ExceptionInfo.Reason"] = reason;
			message.Headers["NServiceBus.ExceptionInfo.ExceptionType"] = e.GetType().FullName;

			if (e.InnerException != null)
				message.Headers["NServiceBus.ExceptionInfo.InnerExceptionType"] = e.InnerException.GetType().FullName;
            
			message.Headers["NServiceBus.ExceptionInfo.HelpLink"] = e.HelpLink;
            message.Headers["NServiceBus.ExceptionInfo.Message"] = e.Message;
            message.Headers["NServiceBus.ExceptionInfo.Source"] = e.Source;
            message.Headers["NServiceBus.ExceptionInfo.StackTrace"] = e.StackTrace;

            message.Headers[HeaderKeys.OriginalId] = message.Id;
            message.Headers[HeaderKeys.FailedQ] = Address.Local.ToString();
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
