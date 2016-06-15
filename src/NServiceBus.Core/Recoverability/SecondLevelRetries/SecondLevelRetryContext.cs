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
        /// The message which failed to process.
        /// </summary>
        public IncomingMessage Message { get; set; }

        /// <summary>
        /// The occured exception.
        /// </summary>
        public Exception Exception { get; set; }

        /// <summary>
        /// The current second level retry attempt.
        /// </summary>
        public int SecondLevelRetryAttempt { get; set; }
    }
}