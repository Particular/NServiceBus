using System;
using NHibernate;
using NServiceBus.ObjectBuilder;
using NServiceBus.SagaPersisters.NHibernate;
using Common.Logging;

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
            return NHibernateSagaPersister(config, sessionFactory, false);
        }

        /// <summary>
        /// Use the NHibernate backed saga persister implementation.
        /// Be aware that this implementation deletes sagas that complete so as not to have the database fill up.
        /// 
        /// Requires that a session factory be configured externally and passed in.
        /// Session factory is then registered as a singleton in the container.
        /// </summary>
        /// <param name="config"></param>
        /// <param name="sessionFactory"></param>
        /// <param name="suppressExceptions">When true prevents throwing exceptions.</param>
        /// <returns></returns>
        public static Configure NHibernateSagaPersister(this Configure config, ISessionFactory sessionFactory, bool suppressExceptions)
        {
            var impl = sessionFactory as NHibernate.Impl.SessionFactoryImpl;
            if (impl != null)
            {
                foreach (Type sagaDataType in Saga.Configure.GetSagaDataTypes())
                {
                    var entityPersister = impl.GetEntityPersister(sagaDataType.FullName);
                    if (entityPersister == null)
                        continue;

                    logger.Debug(string.Format("Checking that saga data {0} has Id generator 'assigned'.", sagaDataType.Name));

                    if (!(entityPersister.IdentifierGenerator is NHibernate.Id.Assigned))
                        if (!suppressExceptions)
                            throw new ConfigurationException(string.Format("Saga type should have Id configured as 'assigned' - {0}.", sagaDataType.Name));
                }
            }
          

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
            return NHibernateSagaPersister(config, false);
        }

        /// <summary>
        /// Use the NHibernate backed saga persister implementation.
        /// Be aware that this implementation deletes sagas that complete so as not to have the database fill up.
        /// 
        /// Independently instantiates NHibernate.Cfg.Configuration, calls Configure(), builds session factory, and registers it in the container.
        /// </summary>
        /// <param name="config"></param>
        /// <param name="suppressExceptions">When true prevents throwing exceptions.</param>
        /// <returns></returns>
        public static Configure NHibernateSagaPersister(this Configure config, bool suppressExceptions)
        {
            var cfg = new NHibernate.Cfg.Configuration()
                .SetProperty(NHibernate.Cfg.Environment.ProxyFactoryFactoryClass,
                             "NHibernate.ByteCode.LinFu.ProxyFactoryFactory, NHibernate.ByteCode.LinFu")
                .SetProperty(NHibernate.Cfg.Environment.CurrentSessionContextClass,
                             "NHibernate.Context.ThreadStaticSessionContext, NHibernate");

            cfg.Configure();

            var sessionFactory = cfg.BuildSessionFactory();

            return NHibernateSagaPersister(config, sessionFactory, suppressExceptions);
        }

        private static ILog logger = LogManager.GetLogger(typeof(SagaPersister));
    }
}
