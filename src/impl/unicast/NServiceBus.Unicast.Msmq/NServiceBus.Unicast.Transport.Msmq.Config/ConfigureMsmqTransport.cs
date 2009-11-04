using NServiceBus.Unicast.Transport.Msmq.Config;
using NServiceBus.Unicast.Queueing.Msmq.Config;

namespace NServiceBus
{
    /// <summary>
    /// Contains extension methods to NServiceBus.Configure.
    /// </summary>
    public static class ConfigureMsmqTransport
    {
        /// <summary>
        /// Returns MsmqTransport specific configuration settings.
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public static ConfigMsmqTransport MsmqTransport(this Configure config)
        {
            config.MsmqMessageQueue();

            var cfg = new ConfigMsmqTransport();
            cfg.Configure(config);

            return cfg;
        }
    }
}
