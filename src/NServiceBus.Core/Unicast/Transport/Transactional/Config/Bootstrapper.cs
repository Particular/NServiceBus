namespace NServiceBus.Unicast.Transport.Transactional.Config
{
    using System;
    using System.Configuration;
    using Licensing;
    using NServiceBus.Config;
    using Logging;

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
            var msmqTransportConfig = Configure.GetConfigSection<MsmqTransportConfig>();

            if (msmqTransportConfig != null)
                Logger.Warn("'MsmqTransportConfig' section is obsolete. Please update your configuration to use the new 'TransportConfig' section instead. In NServiceBus 4.0 this will be treated as an error");
  
            if (transportConfig != null)
            {
                maximumNumberOfRetries = transportConfig.MaxRetries;
                maximumThroughput = transportConfig.MaximumMessageThroughputPerSecond;

                numberOfWorkerThreadsInAppConfig = transportConfig.MaximumConcurrencyLevel;
                return;
            }

            if (msmqTransportConfig != null)
            {
                maximumNumberOfRetries = msmqTransportConfig.MaxRetries;

                if (!string.IsNullOrWhiteSpace(msmqTransportConfig.InputQueue))
                {
                    throw new
                        ConfigurationErrorsException(
                        string.Format("'InputQueue' entry in 'MsmqTransportConfig' section is obsolete. " +
                                      "By default the queue name is taken from the class namespace where the configuration is declared. " +
                                      "To override it, use .DefineEndpointName() with either a string parameter as queue name or Func<string> parameter that returns queue name. " +
                                      "In this instance, '{0}' is defined as queue name.", Configure.EndpointName));
                }

                numberOfWorkerThreadsInAppConfig = msmqTransportConfig.NumberOfWorkerThreads;
                return;
            }
            numberOfWorkerThreadsInAppConfig =  1;
        }

        private int maximumThroughput, maximumNumberOfRetries, numberOfWorkerThreadsInAppConfig;

        static readonly ILog Logger = LogManager.GetLogger(typeof(Bootstrapper));

    }
}
