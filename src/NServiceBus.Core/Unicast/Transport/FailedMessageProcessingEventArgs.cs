namespace NServiceBus.Unicast.Transport
{
    using System;

    /// <summary>
    /// Defives the event data for the failed message processing event
    /// </summary>
    public class FailedMessageProcessingEventArgs : EventArgs
    {
        /// <summary>
        /// The exception that caused the processing to fail
        /// </summary>
        public Exception Reason { get; private set; }

        /// <summary>
        /// Initialized the event arg with the actual exception
        /// </summary>
        /// <param name="ex"></param>
        public FailedMessageProcessingEventArgs(Exception ex)
        {
            Reason = ex;
        }
    }
}