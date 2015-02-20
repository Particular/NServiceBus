namespace NServiceBus.Settings.Throttling
{
    using System;
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
            if (maximumMessagesPerSecond <= 0)
            {
                throw new InvalidOperationException("The maximum messages per second value must be positive.");
            }
            config.Settings.Set<IThrottlingConfig>(new SharedLimitThrottlingConfig(maximumMessagesPerSecond));
        }

        /// <summary>
        /// Congigures NServiceBus to not limit message throughput by default but allows to override this setting for either the main pipeline each satellite individaully.
        /// </summary>
        /// <returns></returns>
        public IndividualThrottlingSettings UseSeparateThroughputLimitForMainPaipelineAndEachSetellite()
        {
            var overrides = new Dictionary<string, int?>();
            var throttlingConfig = new IndividualLimitThrottlingConfig(null, overrides);
            config.Settings.Set<IThrottlingConfig>(throttlingConfig);
            return new IndividualThrottlingSettings(overrides);
        }


        /// <summary>
        /// Congigures NServiceBus to limit message throughput to <paramref name="defaultMaximumMessagesPerSecond"/> by default for each pipeline but allows to override this setting for either the main pipeline each satellite individaully.
        /// </summary>
        /// <param name="defaultMaximumMessagesPerSecond">Default maximum messages per second</param>
        /// <returns></returns>
        public IndividualThrottlingSettings UseSeparateThroughputLimitForMainPaipelineAndEachSetellite(int defaultMaximumMessagesPerSecond)
        {
            if (defaultMaximumMessagesPerSecond <= 0)
            {
                throw new InvalidOperationException("The maximum messages per second value must be positive.");
            }
            var overrides = new Dictionary<string, int?>();
            var throttlingConfig = new IndividualLimitThrottlingConfig(defaultMaximumMessagesPerSecond, overrides);
            config.Settings.Set<IThrottlingConfig>(throttlingConfig);
            return new IndividualThrottlingSettings(overrides);
        }
    }
}