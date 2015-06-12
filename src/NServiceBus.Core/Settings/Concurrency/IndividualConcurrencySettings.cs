namespace NServiceBus.Settings.Concurrency
{
    using System.Collections.Generic;

    /// <summary>
    /// Allows to configure maximum concurrency level separately for the main pipeline and each satellite.
    /// </summary>
    public class IndividualConcurrencySettings
    {
        Dictionary<string, int> overrides;

        internal IndividualConcurrencySettings(Dictionary<string, int> overrides)
        {
            this.overrides = overrides;
        }

        /// <summary>
        /// Configures NServiceBus to use <paramref name="maximumConcurrency"/> concurrency level for the main processing pipeline.
        /// </summary>
        public IndividualConcurrencySettings ForMainPipeline(int maximumConcurrency)
        {
            Guard.AgainstNegativeAndZero(maximumConcurrency, "maximumConcurrency");
            overrides["Main"] = maximumConcurrency;
            return this;
        }

        /// <summary>
        /// Configures NServiceBus to use <paramref name="maximumConcurrency"/> concurrency level for satellite <paramref name="satelliteId"/>.
        /// </summary>
        public IndividualConcurrencySettings ForSatellite(string satelliteId, int maximumConcurrency)
        {
            Guard.AgainstNegativeAndZero(maximumConcurrency, "maximumConcurrency");
            Guard.AgainstNullAndEmpty(satelliteId, "satelliteId");
            overrides[satelliteId] = maximumConcurrency;
            return this;
        }
    }
}