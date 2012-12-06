namespace NServiceBus.Unicast.Transport.Transactional.Config
{
    using System;
    using System.Configuration;
    using Licensing;
    using NServiceBus.Config;
    using DequeueStrategies;
    using DequeueStrategies.ThreadingStrategies;
    using Logging;
    using Queuing;

    public class Bootstrapper : INeedInitialization
    {
        static Bootstrapper()
        {
            TransactionSettings = new TransactionSettings();
        }

        public void Init()
        {
            var numberOfWorkerThreadsInAppConfig = ConfiguredMaxDegreeOfParallelism();
            var throughput = MaxThroughput;

            if (LicenseManager.CurrentLicense.MaxThroughputPerSecond > 0)
            {
                if (throughput == 0 || LicenseManager.CurrentLicense.MaxThroughputPerSecond < throughput)
                    throughput = LicenseManager.CurrentLicense.MaxThroughputPerSecond;
            }

            Configure.Instance.Configurer.ConfigureComponent<TransactionalTransport>(DependencyLifecycle.InstancePerCall)
                .ConfigureProperty(t => t.TransactionSettings, TransactionSettings)
                .ConfigureProperty(t => t.NumberOfWorkerThreads, GetAllowedNumberOfThreads(numberOfWorkerThreadsInAppConfig))
                .ConfigureProperty(t=>t.MaxThroughputPerSecond,throughput);
        }

        static int GetAllowedNumberOfThreads(int numberOfWorkerThreadsInConfig)
        {
            int workerThreadsInLicenseFile = LicenseManager.CurrentLicense.AllowedNumberOfThreads;

            return Math.Min(workerThreadsInLicenseFile, numberOfWorkerThreadsInConfig);
        }

        static int ConfiguredMaxDegreeOfParallelism()
        {
        
            var transportConfig = Configure.GetConfigSection<TransportConfig>();
            var msmqTransportConfig = Configure.GetConfigSection<MsmqTransportConfig>();

            if (msmqTransportConfig != null)
                Logger.Warn("'MsmqTransportConfig' section is obsolete. Please update your configuration to use the new 'TransportConfig' section instead. In NServiceBus 4.0 this will be treated as an error");
  
            if (transportConfig != null)
            {
                TransactionSettings.MaxRetries = transportConfig.MaxRetries;
                MaxThroughput = transportConfig.MaxMessageThroughputPerSecond;

                return transportConfig.MaxDegreeOfParallelism;
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

        public static int MaxThroughput { get; set; }
        public static TransactionSettings TransactionSettings { get; set; }

        static readonly ILog Logger = LogManager.GetLogger("Configuration");

    }

    class DefaultDequeueStrategy : IWantToRunBeforeConfigurationIsFinalized
    {
        public void Run()
        {
            if (Configure.Instance.Configurer.HasComponent<IDequeueMessages>())
                return;

            if (!Configure.Instance.Configurer.HasComponent<IReceiveMessages>())
                throw new InvalidOperationException("No message receiver has been specified. Either configure one or add your own DequeueStrategy");

            Configure.Instance.Configurer.ConfigureComponent<StaticThreadingStrategy>(DependencyLifecycle.InstancePerCall);
            Configure.Instance.Configurer.ConfigureComponent<PollingDequeueStrategy>(DependencyLifecycle.InstancePerCall);
        }
    }
}
