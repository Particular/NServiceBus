namespace NServiceBus
{
    /// <summary>
    /// Provides information about the fault configuration.
    /// </summary>
    public struct FailedConfig
    {
        internal FailedConfig(string errorQueue)
        {
            ErrorQueue = errorQueue;
        }

        /// <summary>
        /// Gets the configured standard error queue.
        /// </summary>
        public string ErrorQueue { get; }
    }
}