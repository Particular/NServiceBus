using System;
using System.Configuration;
using System.Transactions;
using System.Transactions.Configuration;
using NServiceBus.Unicast.Transport.Transactional.Config;

namespace NServiceBus
{
    /// <summary>
    /// Contains extension methods to NServiceBus.Configure
    /// </summary>
    public static class TransactionalConfigManager
    {
        /// <summary>
        /// Sets the transactionality of the endpoint.
        /// If true, the endpoint will not lose messages when exceptions occur.
        /// If false, the endpoint may lose messages when exceptions occur.
        /// </summary>
        /// <param name="config"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static Configure IsTransactional(this Configure config, bool value)
        {
            Bootstrapper.TransactionSettings.IsTransactional = value;
            return config;
        }

        /// <summary>
        /// Sets the transactionality of the endpoint such that 
        /// the endpoint will not lose messages when exceptions occur.
        /// 
        /// Is equivalent to IsTransactional(true);
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public static Configure DontUseTransactions(this Configure config)
        {
            Bootstrapper.TransactionSettings.IsTransactional = false;
            return config;
        }

        /// <summary>
        /// Sets the isolation level that database transactions on this endpoint will run at.
        /// This value is only relevant when IsTransactional has been set to true.
        /// 
        /// Higher levels like RepeatableRead and Serializable promise a higher level
        /// of consistency, but at the cost of lower parallelism and throughput.
        /// 
        /// If you wish to run sagas on this endpoint, RepeatableRead is the suggested value.
        /// </summary>
        /// <param name="config"></param>
        /// <param name="isolationLevel"></param>
        /// <returns></returns>
        public static Configure IsolationLevel(this Configure config, IsolationLevel isolationLevel)
        {
            Bootstrapper. TransactionSettings.IsolationLevel = isolationLevel;
            return config;
        }

        /// <summary>
        /// Sets the time span where a transaction will timeout.
        /// 
        /// Most endpoints should leave it at the default.
        /// </summary>
        /// <param name="config"></param>
        /// <param name="transactionTimeout"></param>
        /// <returns></returns>
        public static Configure TransactionTimeout(this Configure config, TimeSpan transactionTimeout)
        {
            var maxTimeout = GetMaxTimeout();

            if(transactionTimeout > maxTimeout)
                throw new ConfigurationErrorsException("Timeout requested is longer than the maximum value for this machine. Please override using the maxTimeout setting of the system.transactions section in machine.config");

            Bootstrapper.TransactionSettings.TransactionTimeout = transactionTimeout;
         
            return config;
        }

        static TimeSpan GetMaxTimeout()
        {
            //default is 10 always 10 minutes
            var maxTimeout = TimeSpan.FromMinutes(10);

            var systemTransactionsGroup = ConfigurationManager.OpenMachineConfiguration()
                .GetSectionGroup("system.transactions");

            if (systemTransactionsGroup != null)
            {
                var machineSettings = systemTransactionsGroup.Sections.Get("machineSettings") as MachineSettingsSection;
               
                if (machineSettings != null)
                    maxTimeout = machineSettings.MaxTimeout;
            }

            return maxTimeout;
        }
    }
}
