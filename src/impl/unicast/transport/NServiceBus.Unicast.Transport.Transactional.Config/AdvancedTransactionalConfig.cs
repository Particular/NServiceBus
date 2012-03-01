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
        public static Configure SupressDTC(this Configure config)
        {
            Bootstrapper.SupressDTC = true;
            return config;
        }
    }
}