using NServiceBus.Config;

namespace NServiceBus
{
    /// <summary>
    /// Contains extension methods to NServiceBus.Configure.
    /// </summary>
    public static class ConfigureMsmqTransport
    {
        /// <summary>
        /// If queues configured do not exist, will cause them not to be created on startup.
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public static Configure DoNotCreateQueues(this Configure config)
        {
            MsmqTransportConfig.DoNotCreateQueues = true;

            return config;
        }
    }
}
