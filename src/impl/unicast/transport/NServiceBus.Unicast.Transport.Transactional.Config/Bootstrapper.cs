using System;
using System.Transactions;
using NServiceBus.Config;
using NServiceBus.ObjectBuilder;

namespace NServiceBus.Unicast.Transport.Transactional.Config
{
    public class Bootstrapper : INeedInitialization
    {
        public void Init()
        {
            var transportConfig = Configure.Instance.Configurer.ConfigureComponent<TransactionalTransport>(
                ComponentCallModelEnum.Singleton);

            transportConfig.ConfigureProperty(t => t.IsTransactional, IsTransactional);
            transportConfig.ConfigureProperty(t => t.IsolationLevel, IsolationLevel);
            transportConfig.ConfigureProperty(t => t.TransactionTimeout, TransactionTimeout);

            var cfg = Configure.GetConfigSection<MsmqTransportConfig>();

            if (cfg != null)
            {
                transportConfig.ConfigureProperty(t => t.NumberOfWorkerThreads, cfg.NumberOfWorkerThreads);
                transportConfig.ConfigureProperty(t => t.MaxRetries, cfg.MaxRetries);
            }
        }

        public static bool IsTransactional { get; set; }
        public static IsolationLevel IsolationLevel { get; set; }
        public static TimeSpan TransactionTimeout { get; set; }
    }
}
