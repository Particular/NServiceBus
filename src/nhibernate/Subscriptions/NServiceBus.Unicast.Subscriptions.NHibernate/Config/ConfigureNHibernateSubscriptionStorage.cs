using System;
using System.Reflection;
using NHibernate;
using NHibernate.Cfg;
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
        /// <param name="config">The <see cref="Configure" /> object.</param>
        /// <returns>The <see cref="Configure" /> object.</returns>
        public static Configure DBSubscriptionStorageWithSQLiteAndAutomaticSchemaGeneration(this Configure config)
        {
          var configuration = new Configuration()
            .DataBaseIntegration(x =>
            {
              x.Dialect<SQLiteDialect>();
              x.ConnectionString = string.Format(@"Data Source={0};Version=3;New=True;", ".\\NServiceBus.Subscriptions.sqlite");
            });

            return DBSubscriptionStorage(config, configuration, true);
        }

        /// <summary>
        /// Configures the storage with Sqlite as DB and auto generates schema on startup
        /// </summary>
        /// <param name="config">The <see cref="Configure" /> object.</param>
        /// <returns>The <see cref="Configure" /> object.</returns>
        [ObsoleteEx(Replacement = "DBSubscriptionStorageWithSQLiteAndAutomaticSchemaGeneration()", TreatAsErrorFromVersion = "5.0", RemoveInVersion = "6.0")]
        public static Configure DBSubcriptionStorageWithSQLiteAndAutomaticSchemaGeneration(this Configure config)
        {
            return config.DBSubscriptionStorageWithSQLiteAndAutomaticSchemaGeneration();
        }

        /// <summary>
        /// Configures DB Subscription Storage.
        /// Database settings are read from custom config section <see cref="DBSubscriptionStorageConfig"/>.
        /// </summary>
        /// <param name="config">The <see cref="Configure" /> object.</param>
        /// <returns>The <see cref="Configure" /> object.</returns>
        public static Configure DBSubscriptionStorage(this Configure config)
        {
            var configSection = Configure.GetConfigSection<DBSubscriptionStorageConfig>();

            if (configSection == null)
            {
                throw new InvalidOperationException(
                    "No configuration section for DB Subscription Storage found. Please add a DBSubscriptionStorageConfig section to you configuration file");
            }


            if (configSection.NHibernateProperties.Count == 0)
            {
                throw new InvalidOperationException(
                    "No NHibernate properties found. Please specify NHibernateProperties in your DBSubscriptionStorageConfig section");
            }

            return DBSubscriptionStorage(config,
                                        new Configuration().AddProperties(
                                            configSection.NHibernateProperties.ToProperties()),
                                        configSection.UpdateSchema);
        }

        /// <summary>
        /// Configures DB Subscription Storage.
        /// Database settings are read from custom config section <see cref="DBSubscriptionStorageConfig"/>.
        /// </summary>
        /// <param name="config">The <see cref="Configure" /> object.</param>
        /// <returns>The <see cref="Configure" /> object.</returns>
        [ObsoleteEx(Replacement = "DBSubscriptionStorage()", TreatAsErrorFromVersion = "5.0", RemoveInVersion = "6.0")]        
        public static Configure DBSubcriptionStorage(this Configure config)
        {
            return config.DBSubscriptionStorage();
        }

        /// <summary>
        /// Configures the storage with the user supplied persistence configuration
        /// DB schema is updated if requested by the user
        /// </summary>
        /// <param name="config">The <see cref="Configure" /> object.</param>
        /// <param name="configuration">The <see cref="Configuration" /> allows the application to specify properties and mapping documents to be used when creating a <see cref="ISessionFactory" />.</param>
        /// <param name="autoUpdateSchema"><value>True</value> to auto update the database schema.</param>
        /// <returns>The <see cref="Configure" /> object.</returns>
        public static Configure DBSubscriptionStorage(this Configure config,
                                                      Configuration configuration,
                                                      bool autoUpdateSchema)
        {
            var mapper = new ModelMapper();
            mapper.AddMappings(Assembly.GetExecutingAssembly().GetExportedTypes());
            var mappings = mapper.CompileMappingForAllExplicitlyAddedEntities();

            configuration.AddMapping(mappings);

            if (autoUpdateSchema)
                new SchemaUpdate(configuration).Execute(false, true);

            var sessionSource = new SubscriptionStorageSessionProvider(configuration.BuildSessionFactory());

            config.Configurer.RegisterSingleton<ISubscriptionStorageSessionProvider>(sessionSource);
            config.Configurer.ConfigureComponent<SubscriptionStorage>(DependencyLifecycle.InstancePerCall);

            return config;
        }

        /// <summary>
        /// Configures the storage with the user supplied persistence configuration
        /// DB schema is updated if requested by the user
        /// </summary>
        /// <param name="config">The <see cref="Configure" /> object.</param>
        /// <param name="configuration">The <see cref="Configuration" /> allows the application to specify properties and mapping documents to be used when creating a <see cref="ISessionFactory" />.</param>
        /// <param name="autoUpdateSchema"><value>True</value> to auto update the database schema.</param>
        /// <returns>The <see cref="Configure" /> object.</returns>
        [ObsoleteEx(Replacement = "DBSubscriptionStorage(Configuration, bool)", TreatAsErrorFromVersion = "5.0", RemoveInVersion = "6.0")]        
        public static Configure DBSubcriptionStorage(this Configure config,
                                                     Configuration configuration,
                                                     bool autoUpdateSchema)
        {
            return config.DBSubscriptionStorage(configuration, autoUpdateSchema);
        }
    }
}