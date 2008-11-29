using System;
using ObjectBuilder;
using System.Configuration;
using System.Transactions;
using NServiceBus.Config;

namespace NServiceBus.Unicast.Transport.Msmq.Config
{
    public class ConfigMsmqTransport : NServiceBus.Config.Configure
    {
        public ConfigMsmqTransport() : base() { }

        public void Configure(IBuilder builder)
        {
            this.builder = builder;

            transport = builder.ConfigureComponent<MsmqTransport>(ComponentCallModelEnum.Singleton);

            MsmqTransportConfig cfg =
                ConfigurationManager.GetSection("MsmqTransportConfig") as MsmqTransportConfig;

            if (cfg == null)
                throw new ConfigurationErrorsException("Could not find configuration section for Msmq Transport.");

            transport.InputQueue = cfg.InputQueue;
            transport.NumberOfWorkerThreads = cfg.NumberOfWorkerThreads;
            transport.ErrorQueue = cfg.ErrorQueue;
            transport.MaxRetries = cfg.MaxRetries;
        }

        private MsmqTransport transport;


        public ConfigMsmqTransport IsTransactional(bool value)
        {
            transport.IsTransactional = value;
            return this;
        }

        public ConfigMsmqTransport PurgeOnStartup(bool value)
        {
            transport.PurgeOnStartup = value;
            return this;
        }

        public ConfigMsmqTransport IsolationLevel(IsolationLevel isolationLevel)
        {
            transport.IsolationLevel = isolationLevel;
            return this;
        }

        public ConfigMsmqTransport TransactionTimeout(TimeSpan transactionTimeout)
        {
            transport.TransactionTimeout = transactionTimeout;
            return this;
        }
    }
}
