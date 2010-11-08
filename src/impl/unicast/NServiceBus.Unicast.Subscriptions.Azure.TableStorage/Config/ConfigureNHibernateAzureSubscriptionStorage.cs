using System;
using FluentNHibernate;
using FluentNHibernate.Cfg;
using FluentNHibernate.Cfg.Db;
using NHibernate.ByteCode.LinFu;
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

            var database = MsSqlConfiguration.MsSql2005
                .ConnectionString(connectionString)
                .Provider(typeof(TableStorageConnectionProvider).AssemblyQualifiedName)
                .Dialect(typeof(TableStorageDialect).AssemblyQualifiedName)
                .Driver(typeof(TableStorageDriver).AssemblyQualifiedName)
                .ProxyFactoryFactory(typeof(ProxyFactoryFactory).AssemblyQualifiedName);

            var fluentConfiguration = Fluently.Configure()
                .Database(database)
                .Mappings(m => m.FluentMappings.Add(typeof (SubscriptionMap)));
            var configuration = fluentConfiguration.BuildConfiguration();

            var sessionSource = new SessionSource(fluentConfiguration);

            if (createSchema)
            {
                using (var session = sessionSource.CreateSession())
                {
                    new SchemaExport(configuration).Execute(true, true, false, session.Connection, null);
                    session.Flush();
                }
            }

            config.Configurer.RegisterSingleton<ISessionSource>(sessionSource);
            config.Configurer.ConfigureComponent<SubscriptionStorage>(ComponentCallModelEnum.Singlecall);

            return config;

        }        
    }
}