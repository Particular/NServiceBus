namespace NServiceBus
{
    using System;
    using Transports;

    /// <summary>
    /// Contains information available for custom second level retry policies.
    /// </summary>
    public class SecondLevelRetryContext
    {
        /// <summary>
        /// The message that failed to process.
        /// </summary>
        public IncomingMessage Message { get; set; }

        /// <summary>
        /// The exception that occurred.
        /// </summary>
        public Exception Exception { get; set; }

        /// <summary>
        /// The current second level retry attempt.
        /// </summary>
        public int SecondLevelRetryAttempt { get; set; }
    }
}