using System;
using FluentNHibernate;
using FluentNHibernate.Cfg;
using FluentNHibernate.Cfg.Db;
using Microsoft.WindowsAzure.ServiceRuntime;
using NHibernate.ByteCode.LinFu;
using NHibernate.Tool.hbm2ddl;
using NServiceBus.ObjectBuilder;
using NServiceBus.Unicast.Subscriptions.Azure.TableStorage;
using NServiceBus.Unicast.Subscriptions.Azure.TableStorage.Config;
using NHibernate.Drivers.Azure.TableStorage;

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
        public static Configure NHibernateAzureSubcriptionStorage(this Configure config)
        {

            var configSection = Configure.GetConfigSection<NHibernateAzureSubscriptionStorageConfig>();

            if (configSection == null)
            {
                throw new InvalidOperationException("No configuration section for NHibernate Azure Subscription Storage found. Please add a NHibernateAzureSubscriptionStorageConfig section to you configuration file");
            }

            if (configSection.NHibernateProperties.Count  == 0)
            {
                throw new InvalidOperationException("No NHibernate properties found. Please specify NHibernateProperties in your NHibernateAzureSubscriptionStorageConfig section");
            }

            return NHibernateAzureSubcriptionStorage(config,  configSection.CreateSchema);
        }

        /// <summary>
        /// Configures the storage with the user supplied persistence configuration
        /// Azure tables are created if requested by the user
        /// </summary>
        /// <param name="config"></param>
        /// <param name="createSchema"></param>
        /// <returns></returns>
        public static Configure NHibernateAzureSubcriptionStorage(this Configure config,
            bool createSchema)
        {

            var database = MsSqlConfiguration.MsSql2005
                .ConnectionString(RoleEnvironment.GetConfigurationSettingValue("Data.ConnectionString"))
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