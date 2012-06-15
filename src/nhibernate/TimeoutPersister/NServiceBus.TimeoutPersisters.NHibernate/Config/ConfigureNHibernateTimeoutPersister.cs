using System;
using System.Reflection;
using NHibernate.Cfg;
using NHibernate.Cfg.MappingSchema;
using NHibernate.Dialect;
using NHibernate.Mapping.ByCode;
using NHibernate.Tool.hbm2ddl;
using Configuration = NHibernate.Cfg.Configuration;

namespace NServiceBus
{
    using Config;
    using TimeoutPersisters.NHibernate;

    /// <summary>
    /// Configuration extensions for the NHibernate Timeouts persister
    /// </summary>
    public static class ConfigureNHibernateTimeoutPersister
    {
        /// <summary>
        /// Configures the persister with Sqlite as its database and auto generates schema on startup.
        /// </summary>
        /// <param name="config">The configuration object.</param>
        /// <returns>The configuration object.</returns>
        public static Configure UseNHibernateTimeoutPersisterWithSQLiteAndAutomaticSchemaGeneration(this Configure config)
        {
          var configuration = new Configuration()
            .DataBaseIntegration(x =>
            {
              x.Dialect<SQLiteDialect>();
              x.ConnectionString = string.Format(@"Data Source={0};Version=3;New=True;", ".\\NServiceBus.Timeouts.sqlite");
            });

          return UseNHibernateTimeoutPersister(config, configuration, true);
        }

        /// <summary>
        /// Configures NHibernate Timeout Persister.
        /// Database settings are read from custom config section <see cref="TimeoutPersisterConfig"/>.
        /// </summary>
        /// <param name="config">The configuration object.</param>
        /// <returns>The configuration object.</returns>
        public static Configure UseNHibernateTimeoutPersister(this Configure config)
        {

            var configSection = Configure.GetConfigSection<TimeoutPersisterConfig>();

            if (configSection == null)
            {
                throw new InvalidOperationException("No configuration section for DB Subscription Storage found. Please add a DBSubscriptionStorageConfig section to you configuration file");
            }


            if (configSection.NHibernateProperties.Count  == 0)
            {
                throw new InvalidOperationException("No NHibernate properties found. Please specify NHibernateProperties in your DBSubscriptionStorageConfig section");
            }

            return UseNHibernateTimeoutPersister(config,
                new Configuration().AddProperties(configSection.NHibernateProperties.ToProperties()),
                configSection.UpdateSchema);
        }

        /// <summary>
        /// Configures the storage with the user supplied persistence configuration.
        /// Database schema is updated if requested by the user.
        /// </summary>
        /// <param name="config">The configuration object.</param>
        /// <param name="configuration">The <see cref="Configuration"/> object.</param>
        /// <param name="autoUpdateSchema"><value>true</value> to auto update schema<./param>
        /// <returns>The configuration object</returns>
        public static Configure UseNHibernateTimeoutPersister(this Configure config,
            Configuration configuration,
            bool autoUpdateSchema)
        {
          var mapper = new ModelMapper();
          mapper.AddMappings(Assembly.GetExecutingAssembly().GetExportedTypes());
          HbmMapping faultMappings = mapper.CompileMappingForAllExplicitlyAddedEntities();

          configuration.AddMapping(faultMappings);

            if (autoUpdateSchema)
                new SchemaUpdate(configuration).Execute(false, true);

            //var timeoutStorage = new TimeoutStorage(configuration.BuildSessionFactory());

            config.Configurer.ConfigureComponent(
                () => new TimeoutStorage(configuration.BuildSessionFactory()), DependencyLifecycle.SingleInstance);

            return config;
        }
    }
}