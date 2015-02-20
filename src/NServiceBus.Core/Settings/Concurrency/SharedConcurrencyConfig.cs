namespace NServiceBus.Settings.Concurrency
{
    using NServiceBus.Pipeline;

    class SharedConcurrencyConfig : IConcurrencyConfig
    {
        readonly int maximumConcurrencyLevel;

        public SharedConcurrencyConfig(int? maximumConcurrencyLevel)
        {
            this.maximumConcurrencyLevel = maximumConcurrencyLevel ?? 1;
        }

        public IExecutor BuildExecutor(BusNotifications busNotifications)
        {
            return new LimitedThreadPoolExecutor(maximumConcurrencyLevel, busNotifications);
        }
    }
}