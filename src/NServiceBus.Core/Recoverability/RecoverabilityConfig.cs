namespace NServiceBus
{
    /// <summary>
    /// Provides information about the recoverability configuration.
    /// </summary>
    public class RecoverabilityConfig
    {
        /// <summary>
        /// Creates a new recoverability configuration.
        /// </summary>
        /// <param name="immediateConfig">The immediate retries configuration.</param>
        /// <param name="delayedConfig">The delayed retries configuration.</param>
        /// <param name="failedConfig">The failed retries configuration.</param>
        public RecoverabilityConfig(ImmediateConfig immediateConfig, DelayedConfig delayedConfig, FailedConfig failedConfig)
        {
            Guard.AgainstNull(nameof(immediateConfig), immediateConfig);
            Guard.AgainstNull(nameof(delayedConfig), delayedConfig);
            Guard.AgainstNull(nameof(failedConfig), failedConfig);

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