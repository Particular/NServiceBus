namespace NServiceBus
{
    /// <summary>
    /// Provides information about the fault configuration.
    /// </summary>
    public class FailedConfig
    {
        /// <summary>
        /// Creates a new fault configuration.
        /// </summary>
        /// <param name="errorQueue">The address of the error queue.</param>
        public FailedConfig(string errorQueue)
        {
            Guard.AgainstNullAndEmpty(nameof(errorQueue), errorQueue);

            ErrorQueue = errorQueue;
        }

        /// <summary>
        /// Gets the configured standard error queue.
        /// </summary>
        public string ErrorQueue { get; }
    }
}