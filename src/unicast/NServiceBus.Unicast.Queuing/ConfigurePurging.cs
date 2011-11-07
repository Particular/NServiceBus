using NServiceBus.Unicast.Queuing;

namespace NServiceBus
{
    /// <summary>
    /// Configures purging
    /// </summary>
    public static class ConfigurePurging
    {
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
            //config.Configurer.ConfigureProperty<ICanPurgeOnStartup>(t => t.PurgeOnStartup, value);
            //var queue = config.Builder.Build<ICanPurgeOnStartup>();
            //queue.PurgeOnStartup = value;

            return config;
        }
    }
}