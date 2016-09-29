namespace NServiceBus
{
    /// <summary>
    /// Provides information about the immediate retries configuration.
    /// </summary>
    public class ImmediateConfig
    {
        /// <summary>
        /// Creates a new immediate retries configuration.
        /// </summary>
        /// <param name="maxNumberOfRetries">The maximum number of immediate retries.</param>
        public ImmediateConfig(int maxNumberOfRetries)
        {
            Guard.AgainstNegative(nameof(maxNumberOfRetries), maxNumberOfRetries);

            MaxNumberOfRetries = maxNumberOfRetries;
        }

        /// <summary>
        /// Gets the configured maximum number of immediate retries.
        /// </summary>
        /// <remarks>Zero means no retries possible.</remarks>
        public int MaxNumberOfRetries { get; }
    }
}