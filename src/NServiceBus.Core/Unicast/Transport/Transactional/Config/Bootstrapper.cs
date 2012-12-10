namespace NServiceBus.Unicast.Transport.Transactional.Config
{
    using System;
    using System.Configuration;
    using Licensing;
    using NServiceBus.Config;
    using Logging;

    public class Bootstrapper : INeedInitialization
    {
        static Bootstrapper()
        {
            TransactionSettings = new TransactionSettings();
        }

        public void Init()
        {
            var numberOfWorkerThreadsInAppConfig = ConfiguredMaximumConcurrencyLevel();
            var throughput = MaximumThroughput;

            if (LicenseManager.CurrentLicense.MaxThroughputPerSecond > 0)
            {
                if (throughput == 0 || LicenseManager.CurrentLicense.MaxThroughputPerSecond < throughput)
                    throughput = LicenseManager.CurrentLicense.MaxThroughputPerSecond;
            }

            Configure.Instance.Configurer.ConfigureComponent<TransactionalTransport>(DependencyLifecycle.InstancePerCall)
                .ConfigureProperty(t => t.TransactionSettings, TransactionSettings)
                .ConfigureProperty(t => t.MaximumConcurrencyLevel, GetAllowedNumberOfThreads(numberOfWorkerThreadsInAppConfig))
                .ConfigureProperty(t=> t.MaxThroughputPerSecond, throughput);
        }

        static int GetAllowedNumberOfThreads(int numberOfWorkerThreadsInConfig)
        {
            int workerThreadsInLicenseFile = LicenseManager.CurrentLicense.AllowedNumberOfThreads;

            return Math.Min(workerThreadsInLicenseFile, numberOfWorkerThreadsInConfig);
        }

        static int ConfiguredMaximumConcurrencyLevel()
        {
            var transportConfig = Configure.GetConfigSection<TransportConfig>();
            var msmqTransportConfig = Configure.GetConfigSection<MsmqTransportConfig>();

            if (msmqTransportConfig != null)
                Logger.Warn("'MsmqTransportConfig' section is obsolete. Please update your configuration to use the new 'TransportConfig' section instead. In NServiceBus 4.0 this will be treated as an error");
  
            if (transportConfig != null)
            {
                TransactionSettings.MaxRetries = transportConfig.MaxRetries;
                MaximumThroughput = transportConfig.MaximumMessageThroughputPerSecond;

                return transportConfig.MaximumConcurrencyLevel;
            }

            if (msmqTransportConfig != null)
            {
                TransactionSettings.MaxRetries = msmqTransportConfig.MaxRetries;

                if (!string.IsNullOrWhiteSpace(msmqTransportConfig.InputQueue))
                {
                    throw new
                        ConfigurationErrorsException(
                        string.Format("'InputQueue' entry in 'MsmqTransportConfig' section is obsolete. " +
                                      "By default the queue name is taken from the class namespace where the configuration is declared. " +
                                      "To override it, use .DefineEndpointName() with either a string parameter as queue name or Func<string> parameter that returns queue name. " +
                                      "In this instance, '{0}' is defined as queue name.", Configure.EndpointName));
                }

                return msmqTransportConfig.NumberOfWorkerThreads;
            }
            return 1;
        }

        public static int MaximumThroughput { get; set; }
        public static TransactionSettings TransactionSettings { get; set; }

        static readonly ILog Logger = LogManager.GetLogger("Configuration");

    }
}
