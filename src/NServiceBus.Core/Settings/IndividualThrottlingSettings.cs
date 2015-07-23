namespace NServiceBus
{
    using System.Collections.Generic;

    /// <summary>
    /// Allows to configure throttling separately for the main pipeline and each satellite.
    /// </summary>
    public class IndividualThrottlingSettings
    {
        Dictionary<string, int?> overrides;

        internal IndividualThrottlingSettings(Dictionary<string, int?> overrides)
        {
            Guard.AgainstNull(overrides, "overrides");
            this.overrides = overrides;
        }

        /// <summary>
        /// Configures NServiceBus to limit throughput to <paramref name="maximumMessagesPerSecond"/> messages per second for the main processing pipeline.
        /// </summary>
        public IndividualThrottlingSettings ForMainPipeline(int maximumMessagesPerSecond)
        {
            Guard.AgainstNegativeAndZero(maximumMessagesPerSecond, "maximumMessagesPerSecond");
            overrides["Main"] = maximumMessagesPerSecond;
            return this;
        }
        
        /// <summary>
        /// Configures NServiceBus to limit throughput to <paramref name="maximumMessagesPerSecond"/> messages per second for satellite <paramref name="satelliteId"/>.
        /// </summary>
        public IndividualThrottlingSettings ForSatellite(string satelliteId, int maximumMessagesPerSecond)
        {
            Guard.AgainstNegativeAndZero(maximumMessagesPerSecond, "maximumMessagesPerSecond");
            Guard.AgainstNullAndEmpty(satelliteId, "satelliteId");
            overrides[satelliteId] = maximumMessagesPerSecond;
            return this;
        }

        /// <summary>
        /// Configures NServiceBus to not limit throughput for the main processing pipeline.
        /// </summary>
        public IndividualThrottlingSettings DoNotLimitMainPipeline()
        {
            overrides["Main"] = null;
            return this;
        }

        /// <summary>
        /// Configures NServiceBus to not limit throughput for satellite <paramref name="satelliteId"/>.
        /// </summary>
        public IndividualThrottlingSettings DoNotLimit(string satelliteId)
        {
            Guard.AgainstNullAndEmpty(satelliteId, "satelliteId");
            overrides[satelliteId] = null;
            return this;
        }
    }
}