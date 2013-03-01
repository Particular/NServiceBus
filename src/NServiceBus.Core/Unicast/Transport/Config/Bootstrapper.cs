namespace NServiceBus.Unicast.Transport.Config
{
    using System;
    using System.Configuration;
    using Licensing;
    using NServiceBus.Config;
    using INeedInitialization = INeedInitialization;

    public class Bootstrapper : INeedInitialization
    {
        public void Init()
        {
            ConfiguredMaximumConcurrencyLevel();

            if (LicenseManager.CurrentLicense.MaxThroughputPerSecond > 0)
            {
                if (maximumThroughput == 0 || LicenseManager.CurrentLicense.MaxThroughputPerSecond < maximumThroughput)
                    maximumThroughput = LicenseManager.CurrentLicense.MaxThroughputPerSecond;
            }

            var transactionSettings = new Transport.TransactionSettings
                {
                 MaxRetries = maximumNumberOfRetries   
                };

            Configure.Instance.Configurer.ConfigureComponent<TransportReceiver>(DependencyLifecycle.InstancePerCall)
                .ConfigureProperty(t => t.TransactionSettings, transactionSettings)
                .ConfigureProperty(t => t.MaximumConcurrencyLevel, GetAllowedNumberOfThreads(numberOfWorkerThreadsInAppConfig))
                .ConfigureProperty(t => t.MaxThroughputPerSecond, maximumThroughput);
        }

        static int GetAllowedNumberOfThreads(int numberOfWorkerThreadsInConfig)
        {
            int workerThreadsInLicenseFile = LicenseManager.CurrentLicense.AllowedNumberOfThreads;

            return Math.Min(workerThreadsInLicenseFile, numberOfWorkerThreadsInConfig);
        }

        void ConfiguredMaximumConcurrencyLevel()
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
                throw new ConfigurationErrorsException("MsmqTransportConfig has been obsoleted, please use the <TransportConfig> section instead");

            numberOfWorkerThreadsInAppConfig =  1;
        }

        int maximumThroughput, maximumNumberOfRetries, numberOfWorkerThreadsInAppConfig;
    }
}
