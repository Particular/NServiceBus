namespace NServiceBus
{
    using Transport;

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
        /// Details of the exception that occurred.
        /// </summary>
        public ExceptionInfo ExceptionInfo { get; set; }

        /// <summary>
        /// The current second level retry attempt.
        /// </summary>
        public int SecondLevelRetryAttempt { get; set; }
    }
}