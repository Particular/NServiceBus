using NServiceBus.Faults.InMemory;
using NServiceBus.ObjectBuilder;

namespace NServiceBus
{
    /// <summary>
    /// Contains extension methods to NServiceBus.Configure
    /// </summary>
    public static class ConfigureInMemoryFaultManagement
    {
        /// <summary>
        /// Use in-memory fault management.
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public static Configure InMemoryFaultManagement(this Configure config)
        {
            config.Configurer.ConfigureComponent<FaultManager>(ComponentCallModelEnum.Singleton);

            return config;
        }
    }
}
