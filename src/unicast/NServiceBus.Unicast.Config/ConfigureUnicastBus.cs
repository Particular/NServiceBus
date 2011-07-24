using NServiceBus.Unicast.Config;

namespace NServiceBus
{
    /// <summary>
    /// Contains extension methods to NServiceBus.Configure.
    /// </summary>
    public static class ConfigureUnicastBus
    {
        /// <summary>
        /// Use unicast messaging (your best option on nServiceBus right now).
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public static ConfigUnicastBus UnicastBus(this Configure config)
        {
            if (cfg == null)
            {
                cfg = new ConfigUnicastBus();
                cfg.Configure(config);
            }

            return cfg;
        }

        private static ConfigUnicastBus cfg;
    }
}
