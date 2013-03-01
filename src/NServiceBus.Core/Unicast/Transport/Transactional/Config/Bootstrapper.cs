namespace NServiceBus.Unicast.Transport.Transactional.Config
{
    using System;
    using System.Configuration;
    using Licensing;
    using NServiceBus.Config;

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

            Configure.Instance.Configurer.ConfigureComponent<TransactionalTransport>(DependencyLifecycle.InstancePerCall)
                .ConfigureProperty(t => t.MaximumNumberOfRetries, maximumNumberOfRetries)
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
