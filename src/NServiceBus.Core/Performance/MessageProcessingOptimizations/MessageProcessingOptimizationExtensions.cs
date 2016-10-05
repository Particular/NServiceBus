namespace NServiceBus
{
    /// <summary>
    /// Configuration class for durable messaging.
    /// </summary>
    public static class MessageProcessingOptimizationExtensions
    {
        /// <summary>
        /// Instructs the transport to limits the allowed concurrency when processing messages.
        /// </summary>
        /// <param name="config">The <see cref="EndpointConfiguration" /> instance to apply the settings to.</param>
        /// <param name="maxConcurrency">The max concurrency allowed.</param>
        public static void LimitMessageProcessingConcurrencyTo(this EndpointConfiguration config, int maxConcurrency)
        {
            Guard.AgainstNull(nameof(config), config);
            Guard.AgainstNegativeAndZero(nameof(maxConcurrency), maxConcurrency);

            config.Settings.Set<ConcurrencyLimit>(new ConcurrencyLimit
            {
                MaxValue = maxConcurrency
            });
        }

        internal class ConcurrencyLimit
        {
            public int MaxValue { get; set; }
        }
    }
}