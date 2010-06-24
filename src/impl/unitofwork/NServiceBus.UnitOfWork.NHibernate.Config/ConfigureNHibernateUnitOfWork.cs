using NServiceBus.UnitOfWork.NHibernate;
using NServiceBus.ObjectBuilder;

namespace NServiceBus
{
    /// <summary>
    /// Contains extension methods to NServiceBus.Configure for the NHibernate unit of work manager.
    /// </summary>
    public static class ConfigureNHibernateUnitOfWork
    {
        /// <summary>
        /// Use the NHibernate backed unit of work implementation.
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public static Configure NHibernateUnitOfWork(this Configure config)
        {
            config.Configurer.ConfigureComponent<UnitOfWorkManager>(ComponentCallModelEnum.Singleton);
            return config;
        }
    }
}
