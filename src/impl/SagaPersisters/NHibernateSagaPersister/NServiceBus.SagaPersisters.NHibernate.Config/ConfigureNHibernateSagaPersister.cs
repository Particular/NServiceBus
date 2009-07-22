using System;
using FluentNHibernate.Cfg;
using FluentNHibernate.Cfg.Db;
using NHibernate;
using NHibernate.Tool.hbm2ddl;
using NServiceBus.ObjectBuilder;
using NServiceBus.SagaPersisters.NHibernate;
using Common.Logging;
using NServiceBus.SagaPersisters.NHibernate.AutoPersistence;

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
        [Obsolete("Use the fluentconfiguration instead")]
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
        [Obsolete("Use the fluentconfiguration instead")]
        public static Configure NHibernateSagaPersister(this Configure config, bool suppressExceptions)
        {
            var sessionFactory = new NHibernate.Cfg.Configuration()
                                            .Configure() 
                                            .BuildSessionFactory();

            return NHibernateSagaPersister(config, sessionFactory, suppressExceptions);
        }

        /// <summary>
        /// Automatically maps all saga entities using conventions and persists them in the specified DB
        /// Schema is automatically updated on startup
        /// </summary>
        /// <param name="config"></param>
        /// <param name="databaseConfiguration"></param>
        /// <returns></returns>
        public static Configure NHibernateSagaPersister(this Configure config, IPersistenceConfigurer databaseConfiguration)
        {
            return NHibernateSagaPersister(config, databaseConfiguration, true);
        }

        /// <summary>
        /// Automatically maps all saga entities using conventions and persists them in the specified DB
        /// Schema is updated based on user preference 
        /// </summary>
        /// <param name="config"></param>
        /// <param name="databaseConfiguration"></param>
        /// <param name="buildSchema"></param>
        /// <returns></returns>
        public static Configure NHibernateSagaPersister(this Configure config, IPersistenceConfigurer databaseConfiguration, bool buildSchema)
        {
            var model = Create.SagaPersistenceModel();

            var fluentConfiguration = Fluently.Configure()
                                                .Mappings(m => m.AutoMappings.Add(model))
                                                .Database(databaseConfiguration);


            return NHibernateSagaPersister(config, fluentConfiguration, buildSchema);
        }

        /// <summary>
        /// Configures the saga persister with the given configuration. Proxy defaults to Linfu 
        /// if not specified by the user
        /// </summary>
        /// <param name="config"></param>
        /// <param name="fluentConfiguration"></param>
        /// <param name="updateSchema"></param>
        /// <returns></returns>
        public static Configure NHibernateSagaPersister(this Configure config, FluentConfiguration fluentConfiguration, bool updateSchema)
        {
            fluentConfiguration.ExposeConfiguration(
                c =>
                {
                    c.SetProperty("current_session_context_class",
                                  "NHibernate.Context.ThreadStaticSessionContext, NHibernate");

                    //default to LinFu if not specifed by user
                    //if (!c.Properties.Keys.Contains(PROXY_FACTORY_KEY))
                        c.SetProperty(PROXY_FACTORY_KEY,
                                          "NHibernate.ByteCode.LinFu.ProxyFactoryFactory, NHibernate.ByteCode.LinFu");



                }
                );

            var configuration = fluentConfiguration.BuildConfiguration();

            if (updateSchema)
            {
                logger.Info("Building schema");

                new SchemaUpdate(configuration)
                        .Execute(false, true);



            }
            return NHibernateSagaPersister(config, fluentConfiguration.BuildSessionFactory());
        }

        private const string PROXY_FACTORY_KEY = "proxyfactory.factory_class";

        private static readonly ILog logger = LogManager.GetLogger(typeof(SagaPersister));
    }
}
