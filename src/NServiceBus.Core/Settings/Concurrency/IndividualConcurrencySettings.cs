namespace NServiceBus.Settings.Concurrency
{
    using System.Collections.Generic;

    /// <summary>
    /// Allows to configure maximum concurrency level separately for the main pipeline and each satellite.
    /// </summary>
    public class IndividualConcurrencySettings
    {
        readonly Dictionary<string, int> overrides;

        internal IndividualConcurrencySettings(Dictionary<string, int> overrides)
        {
            this.overrides = overrides;
        }

        /// <summary>
        /// Configures NServiceBus to use <paramref name="maximumConcurrency"/> concurrency level for the main processing pipeline.
        /// </summary>
        /// <param name="maximumConcurrency"></param>
        /// <returns></returns>
        public IndividualConcurrencySettings ForMainPipeline(int maximumConcurrency)
        {
            overrides["Main"] = maximumConcurrency;
            return this;
        }

        /// <summary>
        /// Configures NServiceBus to use <paramref name="maximumConcurrency"/> concurrency level for satellite <paramref name="satelliteId"/>.
        /// </summary>
        /// <param name="satelliteId"></param>
        /// <param name="maximumConcurrency"></param>
        /// <returns></returns>
        public IndividualConcurrencySettings ForSatellite(string satelliteId, int maximumConcurrency)
        {
            overrides[satelliteId] = maximumConcurrency;
            return this;
        }
    }
}