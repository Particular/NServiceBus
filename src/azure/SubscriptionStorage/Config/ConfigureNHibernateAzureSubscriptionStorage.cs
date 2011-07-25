using System;
using System.Reflection;
using NHibernate.Cfg;
using NHibernate.Cfg.MappingSchema;
using NHibernate.Mapping.ByCode;
using NHibernate.Tool.hbm2ddl;
using NServiceBus.ObjectBuilder;
using NServiceBus.Unicast.Subscriptions.Azure.TableStorage;
using NServiceBus.Unicast.Subscriptions.Azure.TableStorage.Config;
using NHibernate.Drivers.Azure.TableStorage;
using NServiceBus.Config;

namespace NServiceBus
{
    /// <summary>
    /// Configuration extensions for the NHibernate subscription storage
    /// </summary>
    public static class ConfigureNHibernateAzureSubscriptionStorage
    {
        /// <summary>
        /// Configures NHibernate Azure Subscription Storage , Settings etc are read from custom config section
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public static Configure AzureSubcriptionStorage(this Configure config)
        {

            var configSection = Configure.GetConfigSection<AzureSubscriptionStorageConfig>();

            if (configSection == null)
            {
                throw new InvalidOperationException("No configuration section for NHibernate Azure Subscription Storage found. Please add a NHibernateAzureSubscriptionStorageConfig section to you configuration file");
            }

            return AzureSubcriptionStorage(config,  configSection.ConnectionString, configSection.CreateSchema);
        }

        /// <summary>
        /// Configures the storage with the user supplied persistence configuration
        /// Azure tables are created if requested by the user
        /// </summary>
        /// <param name="config"></param>
        /// <param name="connectionString"></param>
        /// <param name="createSchema"></param>
        /// <returns></returns>
        public static Configure AzureSubcriptionStorage(this Configure config,
            string connectionString,
            bool createSchema)
        {

          var cfg = new Configuration()
            .DataBaseIntegration(x =>
                                   {
                                     x.ConnectionString = connectionString;
                                     x.ConnectionProvider<TableStorageConnectionProvider>();
                                     x.Dialect<TableStorageDialect>();
                                     x.Driver<TableStorageDriver>();
                                   });

          var mapper = new ModelMapper();
          mapper.AddMappings(Assembly.GetExecutingAssembly().GetExportedTypes());
          HbmMapping faultMappings = mapper.CompileMappingForAllExplicitlyAddedEntities();

          cfg.AddMapping(faultMappings);
          
          if (createSchema)
          {
            new SchemaExport(cfg).Execute(true, true, false);
          }

          var sessionSource = new SubscriptionStorageSessionProvider(cfg.BuildSessionFactory());

            config.Configurer.RegisterSingleton<ISubscriptionStorageSessionProvider>(sessionSource);

            config.Configurer.ConfigureComponent<SubscriptionStorage>(DependencyLifecycle.InstancePerCall);

            return config;

        }        
    }
}