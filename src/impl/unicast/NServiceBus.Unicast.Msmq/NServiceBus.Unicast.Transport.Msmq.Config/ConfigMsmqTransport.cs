using System;
using NServiceBus.ObjectBuilder;
using System.Transactions;
using NServiceBus.Config;

namespace NServiceBus.Unicast.Transport.Msmq.Config
{
    /// <summary>
    /// Extends the base Configure class with MsmqTransport specific methods.
    /// Reads administrator set values from the MsmqTransportConfig section
    /// of the app.config.
    /// </summary>
    public class ConfigMsmqTransport : Configure
    {
        /// <summary>
        /// Wraps the given configuration object but stores the same 
        /// builder and configurer properties.
        /// </summary>
        /// <param name="config"></param>
        public void Configure(Configure config)
        {
            Builder = config.Builder;
            Configurer = config.Configurer;

            transportConfig = Configurer.ConfigureComponent<MsmqTransport>(ComponentCallModelEnum.Singleton);

            var cfg = GetConfigSection<MsmqTransportConfig>();

            if (cfg != null)
            {
                transportConfig.ConfigureProperty(t => t.InputQueue, cfg.InputQueue);
                transportConfig.ConfigureProperty(t => t.NumberOfWorkerThreads, cfg.NumberOfWorkerThreads);
                transportConfig.ConfigureProperty(t => t.ErrorQueue, cfg.ErrorQueue);
                transportConfig.ConfigureProperty(t => t.MaxRetries, cfg.MaxRetries);
                transportConfig.ConfigureProperty(t => t.UseDeadLetterQueue, cfg.UseDeadLetterQueue);
                transportConfig.ConfigureProperty(t => t.UseJournalQueue, cfg.UseJournalQueue); 
            }
        }

        private IComponentConfig<MsmqTransport> transportConfig;

        /// <summary>
        /// Sets the transactionality of the endpoint.
        /// If true, the endpoint will not lose messages when exceptions occur.
        /// If false, the endpoint may lose messages when exceptions occur.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public ConfigMsmqTransport IsTransactional(bool value)
        {
            transportConfig.ConfigureProperty(t => t.IsTransactional, value);
            return this;
        }

        /// <summary>
        /// Requests that the incoming queue be purged of all messages when the bus is started.
        /// All messages in this queue will be deleted if this is true.
        /// Setting this to true may make sense for certain smart-client applications, 
        /// but rarely for server applications.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public ConfigMsmqTransport PurgeOnStartup(bool value)
        {
            transportConfig.ConfigureProperty(t => t.PurgeOnStartup, value);
            return this;
        }

        /// <summary>
        /// Sets the isolation level that database transactions on this endpoint will run at.
        /// This value is only relevant when IsTransactional has been set to true.
        /// 
        /// Higher levels like RepeatableRead and Serializable promise a higher level
        /// of consistency, but at the cost of lower parallelism and throughput.
        /// 
        /// If you wish to run sagas on this endpoint, RepeatableRead is the suggested value
        /// and is the default value.
        /// </summary>
        /// <param name="isolationLevel"></param>
        /// <returns></returns>
        public ConfigMsmqTransport IsolationLevel(IsolationLevel isolationLevel)
        {
            transportConfig.ConfigureProperty(t => t.IsolationLevel,isolationLevel);
            return this;
        }

        /// <summary>
        /// If queues configured do not exist, will not cause them
        /// to be created on startup.
        /// </summary>
        /// <returns></returns>
        public ConfigMsmqTransport DoNotCreateQueues()
        {
            transportConfig.ConfigureProperty(t => t.DoNotCreateQueues, true);
            return this;
        }

        /// <summary>
        /// Sets the time span where a transaction will timeout.
        /// 
        /// Most endpoints should leave it at the default.
        /// </summary>
        /// <param name="transactionTimeout"></param>
        /// <returns></returns>
        public ConfigMsmqTransport TransactionTimeout(TimeSpan transactionTimeout)
        {
            transportConfig.ConfigureProperty(t => t.TransactionTimeout, transactionTimeout);
            return this;
        }
    }
}
