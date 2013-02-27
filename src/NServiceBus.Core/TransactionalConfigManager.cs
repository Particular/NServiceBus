namespace NServiceBus
{
    using System;
    using System.Transactions;

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
        [ObsoleteEx(Replacement = "Configure.Transactions.Enable() or Configure.Transactions.Disable()", TreatAsErrorFromVersion = "5.0", RemoveInVersion = "6.0")]                
        public static Configure IsTransactional(this Configure config, bool value)
        {
            if (value)
            {
                Configure.Transactions.Enable();
            }
            else
            {
                Configure.Transactions.Disable();
            }

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
        [ObsoleteEx(Replacement = "Configure.Transactions.Disable()", TreatAsErrorFromVersion = "5.0", RemoveInVersion = "6.0")]
        public static Configure DontUseTransactions(this Configure config)
        {
            Configure.Transactions.Disable();

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
        [ObsoleteEx(Replacement = "Configure.Transactions.Advanced()", TreatAsErrorFromVersion = "5.0", RemoveInVersion = "6.0")]        
        public static Configure IsolationLevel(this Configure config, IsolationLevel isolationLevel)
        {
            Configure.Transactions.Advanced(settings => settings.IsolationLevel(isolationLevel));

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
        [ObsoleteEx(Replacement = "Configure.Transactions.Advanced()", TreatAsErrorFromVersion = "5.0", RemoveInVersion = "6.0")]                
        public static Configure TransactionTimeout(this Configure config, TimeSpan transactionTimeout)
        {
            Configure.Transactions.Advanced(settings => settings.DefaultTimeout(transactionTimeout));

            return config;
        }
    }
}
