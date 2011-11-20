using System;
using System.Transactions;
using NServiceBus.Config;
using NServiceBus.ObjectBuilder;

namespace NServiceBus.Unicast.Transport.Transactional.Config
{
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

            var cfg = Configure.GetConfigSection<MsmqTransportConfig>();

            var numberOfWorkerThreads = 1;
            if (cfg != null)
            {
                numberOfWorkerThreads = cfg.NumberOfWorkerThreads;
                transportConfig.ConfigureProperty(t => t.MaxRetries, cfg.MaxRetries);
            }
            transportConfig.ConfigureProperty(t => t.NumberOfWorkerThreads, numberOfWorkerThreads);
        }

        public static bool IsTransactional { get; set; }
        public static IsolationLevel IsolationLevel { get; set; }
        public static TimeSpan TransactionTimeout { get; set; }
    }
}
