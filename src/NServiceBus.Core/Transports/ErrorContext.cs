namespace NServiceBus.Transports
{
    using System;

    /// <summary>
    /// The context for messages that has failed processing.
    /// </summary>
    public class ErrorContext
    {
        /// <summary>
        /// The exception that caused the message to fail.
        /// </summary>
        public Exception Exception { get; private set; }

        /// <summary>
        /// Initializes the error context.
        /// </summary>
        public ErrorContext(Exception exception)
        {
            Exception = exception;
        }
    }
}