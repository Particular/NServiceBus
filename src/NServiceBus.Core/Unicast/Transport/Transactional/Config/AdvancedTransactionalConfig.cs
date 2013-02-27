namespace NServiceBus.Unicast.Transport.Transactional.Config
{
    public static class AdvancedTransactionalConfig
    {
        /// <summary>
        /// Suppress the use of DTC. Can be combined with IsTransactional to turn off
        /// the DTC but still use the retries
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        [ObsoleteEx(Replacement = "Configure.Transactions.Advanced()", TreatAsErrorFromVersion = "4.0", RemoveInVersion = "5.0")]
        public static Configure SupressDTC(this Configure config)
        {
            Configure.Transactions.Advanced(settings => settings.SuppressDistributedTransactions(true));
            return config;
        }
    }
}