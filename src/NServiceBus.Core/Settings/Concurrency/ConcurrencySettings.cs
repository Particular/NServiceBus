namespace NServiceBus.Settings.Concurrency
{
    using System.Collections.Generic;

    /// <summary>
    /// Allows to customize how NServiceBus manages execution threads.
    /// </summary>
    public class ConcurrencySettings
    {
        readonly BusConfiguration config;

        internal ConcurrencySettings(BusConfiguration config)
        {
            this.config = config;
        }

        /// <summary>
        /// Configures NServiceBus to use a single thread pool for both main processing pipeline and all the satellites. 
        /// The pool with have the default maximum concurrency level of 1.
        /// </summary>
        public void UseSingleThreadPool()
        {
            config.Settings.Set<IConcurrencyConfig>(new SharedConcurrencyConfig(null));
        }

        /// <summary>
        /// Configures NServiceBus to use a single thread pool for both main processing pipeline and all the satellites. 
        /// </summary>
        /// <param name="maximumConcurrencyLevel">Maximum concurrency level of the pool.</param>
        public void UseSingleThreadPool(int maximumConcurrencyLevel)
        {
            config.Settings.Set<IConcurrencyConfig>(new SharedConcurrencyConfig(maximumConcurrencyLevel));
        }

        /// <summary>
        /// Configures NServiceBus to use separate thread pools for main processing pipeline and each satellite. 
        /// If not explicitly overridden, each thread pool will have the default concurrency limit of 1.
        /// </summary>
        /// <returns></returns>
        public IndividualConcurrencySettings UseSeparateThreadPoolsForMainPipelineAndEachSetellite()
        {
            var overrides = new Dictionary<string, int>();
            var concurrencyConfig = new IndividualConcurrencyConfig(null, overrides);
            config.Settings.Set<IConcurrencyConfig>(concurrencyConfig);
            return new IndividualConcurrencySettings(overrides);
        }

        /// <summary>
        /// Configures NServiceBus to use separate thread pools for main processing pipeline and each satellite. 
        /// If not explicitly overridden, each thread pool will have the default concurrency limit of <paramref name="defaultMaxiumConcurrencyLevel"/>
        /// </summary>
        /// <param name="defaultMaxiumConcurrencyLevel">Defaut maximum concurrency if not overridden.</param>
        /// <returns></returns>
        public IndividualConcurrencySettings UseSeparateThreadPoolsForMainPipelineAndEachSetellite(int defaultMaxiumConcurrencyLevel)
        {
            var overrides = new Dictionary<string, int>();
            var concurrencyConfig = new IndividualConcurrencyConfig(defaultMaxiumConcurrencyLevel, overrides);
            config.Settings.Set<IConcurrencyConfig>(concurrencyConfig);
            return new IndividualConcurrencySettings(overrides);
        }
    }
}