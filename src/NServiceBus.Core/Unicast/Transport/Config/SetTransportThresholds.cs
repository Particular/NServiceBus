namespace NServiceBus.Unicast.Transport.Config
{
    using NServiceBus.Config;

    class SetTransportThresholds : INeedInitialization
    {
        public void Init()
        {
            var transportConfig = Configure.GetConfigSection<TransportConfig>();
            var maximumThroughput = 0;
            var maximumNumberOfRetries = 5;
            var maximumConcurrencyLevel = 1;

            if (transportConfig != null)
            {
                maximumNumberOfRetries = transportConfig.MaxRetries;
                maximumThroughput = transportConfig.MaximumMessageThroughputPerSecond;
                maximumConcurrencyLevel = transportConfig.MaximumConcurrencyLevel;
            }

            var transactionSettings = new TransactionSettings
                {
                    MaxRetries = maximumNumberOfRetries
                };

            Configure.Instance.Configurer.ConfigureComponent<TransportReceiver>(DependencyLifecycle.InstancePerCall)
                .ConfigureProperty(t => t.TransactionSettings, transactionSettings)
                .ConfigureProperty(t => t.MaximumConcurrencyLevel, maximumConcurrencyLevel)
                .ConfigureProperty(t => t.MaxThroughputPerSecond, maximumThroughput);
        }

    }
}
