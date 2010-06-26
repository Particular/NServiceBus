using System;
using NServiceBus.Unicast.Transport;

namespace NServiceBus.Faults.Forwarder
{
    /// <summary>
    /// Implementation of IManageMessageFailures by forwarding messages
    /// using the bus.
    /// </summary>
    public class FaultManager : IManageMessageFailures
    {
        //Intentionally service-locate the bus to avoid circular
        //resolution problem in the container

        void IManageMessageFailures.SerializationFailedForMessage(TransportMessage message, Exception e)
        {
            NServiceBus.Configure.Instance.Builder.Build<IBus>()
                .Send(ErrorQueue, new SerializationFailedMessage
                {
                    ExceptionInfo = e.GetInfo(),
                    FailedMessage = message
                });
        }

        void IManageMessageFailures.ProcessingAlwaysFailsForMessage(TransportMessage message, Exception e)
        {
            if (SanitizeProcessingExceptions)
            {
                e = ExceptionSanitizer.Sanitize(e);
            }

            NServiceBus.Configure.Instance.Builder.Build<IBus>()
                .Send(ErrorQueue, new ProcessingFailedMessage
                {
                    ExceptionInfo = e.GetInfo(),
                    FailedMessage = message
                });
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
