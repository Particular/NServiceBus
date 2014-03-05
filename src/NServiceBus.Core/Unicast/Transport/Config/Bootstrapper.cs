namespace NServiceBus.Unicast.Transport.Config
{
    using System.Configuration;
    using Licensing;
    using NServiceBus.Config;
    using INeedInitialization = INeedInitialization;

    public class Bootstrapper : INeedInitialization
    {
        public void Init()
        {
            LoadConfigurationSettings();

            if (LicenseManager.License.MaxThroughputPerSecond > 0)
            {
                if (maximumThroughput == 0 || LicenseManager.License.MaxThroughputPerSecond < maximumThroughput)
                    maximumThroughput = LicenseManager.License.MaxThroughputPerSecond;
            }

            var transactionSettings = new TransactionSettings
                {
                    MaxRetries = maximumNumberOfRetries
                };

            Configure.Instance.Configurer.ConfigureComponent<TransportReceiver>(DependencyLifecycle.InstancePerCall)
                .ConfigureProperty(t => t.TransactionSettings, transactionSettings)
                .ConfigureProperty(t => t.MaximumConcurrencyLevel, numberOfWorkerThreadsInAppConfig)
                .ConfigureProperty(t => t.MaxThroughputPerSecond, maximumThroughput);
        }

        void LoadConfigurationSettings()
        {
            var transportConfig = Configure.GetConfigSection<TransportConfig>();

            if (transportConfig != null)
            {
                maximumNumberOfRetries = transportConfig.MaxRetries;
                maximumThroughput = transportConfig.MaximumMessageThroughputPerSecond;

                numberOfWorkerThreadsInAppConfig = transportConfig.MaximumConcurrencyLevel;
                return;
            }

            if (Configure.GetConfigSection<MsmqTransportConfig>() != null)
            {
                throw new ConfigurationErrorsException("'MsmqTransportConfig' section is obsolete. Please update your configuration to use the new 'TransportConfig' section instead. You can use the PowerShell cmdlet 'Add-NServiceBusTransportConfig' in the Package Manager Console to quickly add it for you.");
            }
        }

        int maximumThroughput;
        int maximumNumberOfRetries = 5;
        int numberOfWorkerThreadsInAppConfig = 1;
    }
}
