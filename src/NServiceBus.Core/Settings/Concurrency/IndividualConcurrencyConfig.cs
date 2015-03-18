namespace NServiceBus.Settings.Concurrency
{
    using System.Collections.Generic;
    using NServiceBus.Pipeline;

    class IndividualConcurrencyConfig : IConcurrencyConfig
    {
        readonly int defaultMaximumConcurrencyLevel;
        readonly Dictionary<string, int> concurrencyLevelOverrides;

        public IndividualConcurrencyConfig(int? defaultMaximumConcurrencyLevel, Dictionary<string, int> concurrencyLevelOverrides)
        {
            this.defaultMaximumConcurrencyLevel = defaultMaximumConcurrencyLevel ?? 1;
            this.concurrencyLevelOverrides = concurrencyLevelOverrides;
        }

        public IExecutor BuildExecutor(BusNotifications busNotifications)
        {
            return new IndividualLimitThreadPoolExecutor(defaultMaximumConcurrencyLevel, concurrencyLevelOverrides, busNotifications);
        }
    }
}