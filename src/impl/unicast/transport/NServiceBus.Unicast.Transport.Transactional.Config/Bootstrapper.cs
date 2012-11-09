using System;
using System.Configuration;
using System.Transactions;
using NServiceBus.Config;

namespace NServiceBus.Unicast.Transport.Transactional.Config
{
    using DequeueStrategies;
    using Queuing;

    public class Bootstrapper : INeedInitialization
    {
        static Bootstrapper()
        {
            TransactionSettings = new TransactionSettings();
        }

        public void Init()
        {

            var cfg = Configure.GetConfigSection<MsmqTransportConfig>();

            var numberOfWorkerThreadsInAppConfig = 1;
            if (cfg != null)
            {
                numberOfWorkerThreadsInAppConfig = cfg.NumberOfWorkerThreads;
                TransactionSettings.MaxRetries = cfg.MaxRetries;

                if (!string.IsNullOrWhiteSpace(cfg.InputQueue))
                {
                    throw new
                        ConfigurationErrorsException(string.Format("'InputQueue' entry in 'MsmqTransportConfig' section is obsolete. " +
                        "By default the queue name is taken from the class namespace where the configuration is declared. " +
                        "To override it, use .DefineEndpointName() with either a string parameter as queue name or Func<string> parameter that returns queue name. " +
                        "In this instance, '{0}' is defined as queue name.", Configure.EndpointName));
                }
            }

            Configure.Instance.Configurer.ConfigureComponent<TransactionalTransport>(DependencyLifecycle.SingleInstance)
                .ConfigureProperty(t => t.TransactionSettings, TransactionSettings)
                .ConfigureProperty(t => t.NumberOfWorkerThreads, LicenceConfig.GetAllowedNumberOfThreads(numberOfWorkerThreadsInAppConfig));
        }

        public static TransactionSettings TransactionSettings { get; set; }
    }

    class DefaultDequeueStrategy : IWantToRunBeforeConfigurationIsFinalized
    {
        public void Run()
        {
            if (Configure.Instance.Configurer.HasComponent<IDequeueMessages>())
                return;

            if (!Configure.Instance.Configurer.HasComponent<IReceiveMessages>())
                throw new InvalidOperationException("No message receiver has been specified. Either configure one or add your own DequeueStrategy");

            Configure.Instance.Configurer.ConfigureComponent<PollingDequeueStrategy>(DependencyLifecycle.InstancePerCall);
        }
    }
}
