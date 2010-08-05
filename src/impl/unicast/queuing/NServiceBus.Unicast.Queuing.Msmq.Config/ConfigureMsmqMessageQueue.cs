using NServiceBus.ObjectBuilder;
using NServiceBus.Unicast.Queuing.Msmq;

namespace NServiceBus
{
    public static class ConfigureMsmqMessageQueue
    {
        /// <summary>
        /// Use MSMQ for your queuing infrastructure.
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public static Configure MsmqTransport(this Configure config)
        {
            config.Configurer.ConfigureComponent<MsmqMessageQueue>(ComponentCallModelEnum.Singleton);

            return config;
        }

        /// <summary>
        /// Requests that the incoming queue be purged of all messages when the bus is started.
        /// All messages in this queue will be deleted if this is true.
        /// Setting this to true may make sense for certain smart-client applications, 
        /// but rarely for server applications.
        /// </summary>
        /// <param name="config"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static Configure PurgeOnStartup(this Configure config, bool value)
        {
            Configure.Instance.Configurer.ConfigureProperty<MsmqMessageQueue>(t => t.PurgeOnStartup, value);

            return config;
        }
    }
}
