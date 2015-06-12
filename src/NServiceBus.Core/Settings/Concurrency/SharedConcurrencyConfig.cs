namespace NServiceBus.Settings.Concurrency
{
    using NServiceBus.Pipeline;

    class SharedConcurrencyConfig : IConcurrencyConfig
    {
        int maximumConcurrencyLevel;

        public SharedConcurrencyConfig(int? maximumConcurrencyLevel)
        {
            this.maximumConcurrencyLevel = maximumConcurrencyLevel ?? 1;
        }

        public IExecutor BuildExecutor()
        {
            return new LimitedThreadPoolExecutor(maximumConcurrencyLevel);
        }
    }
}