using FluentNHibernate;
using FluentNHibernate.Cfg;
using FluentNHibernate.Cfg.Db;
using NHibernate.Tool.hbm2ddl;
using NServiceBus.ObjectBuilder;
using NServiceBus.Unicast.Subscriptions.NHibernate;

namespace NServiceBus
{
    /// <summary>
    /// Configuration extensions for the NHibernate subscription storage
    /// </summary>
    public static class ConfigureNHibernateSubscriptionStorage
    {

        /// <summary>
        /// Configures the storage using the user configures sessionsource
        /// </summary>
        /// <param name="config"></param>
        /// <param name="sessionSource"></param>
        /// <returns></returns>
        public static Configure NHibernateSubcriptionStorage(this Configure config, ISessionSource sessionSource)
        {

            config.Configurer.RegisterSingleton<ISessionSource>(sessionSource);
            config.Configurer.ConfigureComponent<SubscriptionStorage>(ComponentCallModelEnum.Singlecall);

            return config;
        }

        /// <summary>
        /// Configures the storage with the user supplied persistence configuration
        /// DB schema is automatically updated if nessesary
        /// </summary>
        /// <param name="config"></param>
        /// <param name="persistenceConfigurer"></param>
        /// <returns></returns>
        public static Configure NHibernateSubcriptionStorage(this Configure config,
                  IPersistenceConfigurer persistenceConfigurer)
        {
            return NHibernateSubcriptionStorage(config, persistenceConfigurer, true);
        }
        /// <summary>
        /// Configures the storage with the user supplied persistence configuration
        /// DB schema is updated if requested by the user
        /// </summary>
        /// <param name="config"></param>
        /// <param name="persistenceConfigurer"></param>
        /// <param name="autoCreateSchema"></param>
        /// <returns></returns>
        public static Configure NHibernateSubcriptionStorage(this Configure config,
            IPersistenceConfigurer persistenceConfigurer,
            bool autoCreateSchema)
        {
            var fluentConfiguration = Fluently.Configure()
              .Database(persistenceConfigurer)
              .Mappings(m => m.FluentMappings.AddFromAssemblyOf<Subscription>());

            var cfg = fluentConfiguration.BuildConfiguration();

            if (autoCreateSchema)
                new SchemaUpdate(cfg)
                    .Execute(false, true);

            //default to LinFu if not specifed by user
            if (!cfg.Properties.Keys.Contains(PROXY_FACTORY_KEY))
                fluentConfiguration.ExposeConfiguration(
                    x =>
                    x.SetProperty(PROXY_FACTORY_KEY,
                                  "NHibernate.ByteCode.LinFu.ProxyFactoryFactory, NHibernate.ByteCode.LinFu"));

            var sessionSource = new SessionSource(fluentConfiguration);


            return config.NHibernateSubcriptionStorage(sessionSource);
        }

        private const string PROXY_FACTORY_KEY = "proxyfactory.factory_class";
    }
}