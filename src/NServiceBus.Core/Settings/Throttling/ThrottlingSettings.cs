namespace NServiceBus.Settings.Throttling
{
    using System.Collections.Generic;

    /// <summary>
    /// Allows to customize how NServiceBus manages execution threads.
    /// </summary>
    public class ThrottlingSettings
    {
        readonly BusConfiguration config;

        internal ThrottlingSettings(BusConfiguration config)
        {
            this.config = config;
        }

        /// <summary>
        /// Configures NServiceBus to not limit message throughput.
        /// </summary>
        public void DoNotLimitThroughput()
        {
            config.Settings.Set<IThrottlingConfig>(new NoLimitThrottlingConfig());
        }

        /// <summary>
        /// Configures NServiceBus to limit message throughput to <paramref name="maximumMessagesPerSecond"/> messages per second total for the main processing pipeline and all the satellites.
        /// </summary>
        /// <param name="maximumMessagesPerSecond">Maximum messages per second</param>
        public void UseSingleTotalThroughputLimit(int maximumMessagesPerSecond)
        {
            Guard.AgainstNegativeAndZero(maximumMessagesPerSecond, "maximumMessagesPerSecond");
            config.Settings.Set<IThrottlingConfig>(new SharedLimitThrottlingConfig(maximumMessagesPerSecond));
        }

        /// <summary>
        /// Configures NServiceBus to not limit message throughput by default but allows to override this setting for either the main pipeline each satellite individually.
        /// </summary>
        public IndividualThrottlingSettings UseSeparateThroughputLimitForMainPipelineAndEachSatellite()
        {
            var overrides = new Dictionary<string, int?>();
            var throttlingConfig = new IndividualLimitThrottlingConfig(null, overrides);
            config.Settings.Set<IThrottlingConfig>(throttlingConfig);
            return new IndividualThrottlingSettings(overrides);
        }


        /// <summary>
        /// Configures NServiceBus to limit message throughput to <paramref name="defaultMaximumMessagesPerSecond"/> by default for each pipeline but allows to override this setting for either the main pipeline each satellite individually.
        /// </summary>
        /// <param name="defaultMaximumMessagesPerSecond">Default maximum messages per second</param>
        public IndividualThrottlingSettings UseSeparateThroughputLimitForMainPipelineAndEachSatellite(int defaultMaximumMessagesPerSecond)
        {
            Guard.AgainstNegativeAndZero(defaultMaximumMessagesPerSecond, "defaultMaximumMessagesPerSecond");
            var overrides = new Dictionary<string, int?>();
            var throttlingConfig = new IndividualLimitThrottlingConfig(defaultMaximumMessagesPerSecond, overrides);
            config.Settings.Set<IThrottlingConfig>(throttlingConfig);
            return new IndividualThrottlingSettings(overrides);
        }
    }
}