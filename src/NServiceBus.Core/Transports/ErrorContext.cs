namespace NServiceBus.Transports
{
    using System;

    /// <summary>
    ///
    /// </summary>
    public class ErrorContext
    {
        /// <summary>
        ///
        /// </summary>
        public Exception Exception { get; }

        /// <summary>
        /// 
        /// </summary>
        public int NumberOfProcessingAttempts { get; }

        /// <summary>
        /// The ID of the message that failed processing.
        /// </summary>
        public string MessageId { get; }

        /// <summary>
        ///
        /// </summary>
        public ErrorContext(Exception exception, int numberOfProcessingAttempts)
        {
            Exception = exception;
            NumberOfProcessingAttempts = numberOfProcessingAttempts;
        }
    }
}