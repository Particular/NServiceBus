namespace NServiceBus
{
    /// <summary>
    /// Provides information about the recoverability configuration.
    /// </summary>
    public struct RecoverabilityConfig
    {
        internal RecoverabilityConfig(ImmediateConfig immediateConfig, DelayedConfig delayedConfig, FailedConfig failedConfig)
        {
            Immediate = immediateConfig;
            Delayed = delayedConfig;
            Failed = failedConfig;
        }

        /// <summary>
        /// Exposes the immediate retries configuration.
        /// </summary>
        public ImmediateConfig Immediate { get; }

        /// <summary>
        /// Exposes the delayed retries configuration.
        /// </summary>
        public DelayedConfig Delayed { get; }

        /// <summary>
        /// Exposes the failed retries configuration.
        /// </summary>
        public FailedConfig Failed { get; }
    }
}