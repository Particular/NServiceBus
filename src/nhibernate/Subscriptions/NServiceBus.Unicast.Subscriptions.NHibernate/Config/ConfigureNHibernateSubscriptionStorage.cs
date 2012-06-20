using System;
using System.Reflection;
using NHibernate.Cfg;
using NHibernate.Cfg.MappingSchema;
using NHibernate.Dialect;
using NHibernate.Mapping.ByCode;
using NHibernate.Tool.hbm2ddl;
using NServiceBus.Config;
using NServiceBus.Unicast.Subscriptions.NHibernate;
using Configuration = NHibernate.Cfg.Configuration;

namespace NServiceBus
{
    /// <summary>
    /// Configuration extensions for the NHibernate subscription storage
    /// </summary>
    public static class ConfigureNHibernateSubscriptionStorage
    {
        /// <summary>
        /// Configures the storage with Sqlite as DB and auto generates schema on startup
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public static Configure DBSubcriptionStorageWithSQLiteAndAutomaticSchemaGeneration(this Configure config)
        {
          var configuration = new Configuration()
            .DataBaseIntegration(x =>
            {
              x.Dialect<SQLiteDialect>();
              x.ConnectionString = string.Format(@"Data Source={0};Version=3;New=True;", ".\\NServiceBus.Subscriptions.sqlite");
            });

            return DBSubcriptionStorage(config, configuration, true);
        }

        /// <summary>
        /// Configures DB Subscription Storage , DB Settings etc are read from custom config section (DBSubscriptionStoreage)
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public static Configure DBSubcriptionStorage(this Configure config)
        {

            var configSection = Configure.GetConfigSection<DBSubscriptionStorageConfig>();

            if (configSection == null)
            {
                throw new InvalidOperationException("No configuration section for DB Subscription Storage found. Pleas add a DBSubscriptionStorageConfig section to you configuration file");
            }


            if (configSection.NHibernateProperties.Count  == 0)
            {
                throw new InvalidOperationException("No NHibernate properties found. Please specify NHibernateProperties in your DBSubscriptionStorageConfig section");
            }

            return DBSubcriptionStorage(config,
                new Configuration().AddProperties(configSection.NHibernateProperties.ToProperties()),
                configSection.UpdateSchema);
        }

        /// <summary>
        /// Configures the storage with the user supplied persistence configuration
        /// DB schema is updated if requested by the user
        /// </summary>
        /// <param name="config"></param>
        /// <param name="nhibernateProperties"></param>
        /// <param name="autoUpdateSchema"></param>
        /// <returns></returns>
        public static Configure DBSubcriptionStorage(this Configure config,
            Configuration configuration,
            bool autoUpdateSchema)
        {
          var mapper = new ModelMapper();
          mapper.AddMappings(Assembly.GetExecutingAssembly().GetExportedTypes());
          HbmMapping faultMappings = mapper.CompileMappingForAllExplicitlyAddedEntities();

          configuration.AddMapping(faultMappings);

            if (autoUpdateSchema)
                new SchemaUpdate(configuration).Execute(false, true);

            var sessionSource = new SubscriptionStorageSessionProvider(configuration.BuildSessionFactory());

            config.Configurer.RegisterSingleton<ISubscriptionStorageSessionProvider>(sessionSource);
            config.Configurer.ConfigureComponent<SubscriptionStorage>(DependencyLifecycle.InstancePerCall);

            return config;
        }
    }
}