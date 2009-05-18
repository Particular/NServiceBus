using System;
using NHibernate;
using NServiceBus.ObjectBuilder;
using NServiceBus.SagaPersisters.NHibernate;

namespace NServiceBus
{
    /// <summary>
    /// Contains extension methods to NServiceBus.Configure for the NHibernate saga persister.
    /// </summary>
    public static class ConfigureNHibernateSagaPersister
    {
        /// <summary>
        /// Use the NHibernate backed saga persister implementation.
        /// Be aware that this implementation deletes sagas that complete so as not to have the database fill up.
        /// 
        /// Requires that a session factory be configured externally and passed in.
        /// Session factory is then registered as a singleton in the container.
        /// </summary>
        /// <param name="config"></param>
        /// <param name="sessionFactory">An externally configured session factory.</param>
        /// <returns></returns>
        public static Configure NHibernateSagaPersister(this Configure config, ISessionFactory sessionFactory)
        {
            config.Configurer.RegisterSingleton<ISessionFactory>(sessionFactory);

            config.Configurer.ConfigureComponent<SagaPersister>(ComponentCallModelEnum.Singlecall);

            config.Configurer.ConfigureComponent<NHibernateMessageModule>(ComponentCallModelEnum.Singleton);

            return config;
        }

        /// <summary>
        /// Use the NHibernate backed saga persister implementation.
        /// Be aware that this implementation deletes sagas that complete so as not to have the database fill up.
        /// 
        /// Independently instantiates NHibernate.Cfg.Configuration, calls Configure(), builds session factory, and registers it in the container.
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public static Configure NHibernateSagaPersister(this Configure config)
        {
            NHibernate.Cfg.Configuration cfg = new NHibernate.Cfg.Configuration();
            cfg.Configure();

            var sessionFactory = cfg.BuildSessionFactory();

            return NHibernateSagaPersister(config, sessionFactory);
        }
    }
}
