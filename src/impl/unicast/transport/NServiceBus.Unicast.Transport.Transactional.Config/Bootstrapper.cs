using System;
using System.Configuration;
using System.Transactions;
using Common.Logging;
using NServiceBus.Config;
using NServiceBus.Licensing;

namespace NServiceBus.Unicast.Transport.Transactional.Config
{
    using Installers;
    public class Bootstrapper : INeedInitialization
    {
        static Bootstrapper()
        {
            IsTransactional = true;
        }

        public void Init()
        {
            var transportConfig = Configure.Instance.Configurer.ConfigureComponent<TransactionalTransport>(
                DependencyLifecycle.SingleInstance);

            transportConfig.ConfigureProperty(t => t.IsTransactional, IsTransactional);
            transportConfig.ConfigureProperty(t => t.IsolationLevel, IsolationLevel);
            transportConfig.ConfigureProperty(t => t.TransactionTimeout, TransactionTimeout);
            transportConfig.ConfigureProperty(t => t.SupressDTC, SupressDTC);

            var cfg = Configure.GetConfigSection<MsmqTransportConfig>();

            var numberOfWorkerThreadsInAppConfig = 1;
            if (cfg != null)
            {
                numberOfWorkerThreadsInAppConfig = cfg.NumberOfWorkerThreads;
                transportConfig.ConfigureProperty(t => t.MaxRetries, cfg.MaxRetries);
                if (!string.IsNullOrWhiteSpace(cfg.InputQueue))
                {
                    throw new
                        ConfigurationErrorsException(string.Format("'InputQueue' entry in 'MsmqTransportConfig' section is obsolete. " +
                        "By default the queue name is taken from the class namespace where the configuration is declared. " +
                        "To override it, use .DefineEndpointName() with either a string parameter as queue name or Func<string> parameter that returns queue name. " +
                        "In this instance, '{0}' is defined as queue name.", Configure.EndpointName));
                }
            }
            // Limit all transactional transport users (gateway, distributer, timeout)
            transportConfig.ConfigureProperty(t => t.NumberOfWorkerThreads, LicenceConfig.GetAllowedNumberOfThreads(numberOfWorkerThreadsInAppConfig));

            DtcInstaller.IsEnabled = IsTransactional;
        }

        public static bool IsTransactional { get; set; }
        public static IsolationLevel IsolationLevel { get; set; }
        public static TimeSpan TransactionTimeout { get; set; }
        public static bool SupressDTC { get; set; }
        public static bool SuppressOutdatedTimeoutDispatchWarning { get; set; }
    }
}
