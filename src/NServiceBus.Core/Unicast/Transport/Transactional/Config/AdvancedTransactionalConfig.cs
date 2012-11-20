namespace NServiceBus.Unicast.Transport.Transactional.Config
{
    using NServiceBus.Config;

    public static class AdvancedTransactionalConfig
    {
        /// <summary>
        /// Suppress the use of DTC. Can be combined with IsTransactional to turn off
        /// the DTC but still use the retries
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        [ObsoleteEx(Replacement = "SuppressDTC()", TreatAsErrorFromVersion = "4.0", RemoveInVersion = "5.0")]
        public static Configure SupressDTC(this Configure config)
        {
            return config.SuppressDTC();
        }

        /// <summary>
        /// Suppress the use of DTC. Can be combined with IsTransactional to turn off
        /// the DTC but still use the retries
        /// </summary>
        /// <param name="config">The <see cref="Configure"/></param>
        /// <returns>The <see cref="Configure"/></returns>
        public static Configure SuppressDTC(this Configure config)
        {
            Endpoint.DontUseDistributedTransactions = true;
            return config;
        }
    }
}