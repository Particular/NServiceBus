namespace NServiceBus
{
    /// <summary>
    /// Provides information about the immediate retries configuration.
    /// </summary>
    public struct ImmediateConfig
    {
        internal ImmediateConfig(int maxNumberOfRetries)
        {
            MaxNumberOfRetries = maxNumberOfRetries;
        }

        /// <summary>
        /// Gets the configured maximum number of immediate retries.
        /// </summary>
        /// <remarks>Zero means no retries possible.</remarks>
        public int MaxNumberOfRetries { get; }
    }
}