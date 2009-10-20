using System;
using System.Collections.Generic;
using FluentNHibernate;
using FluentNHibernate.Cfg;
using FluentNHibernate.Cfg.Db;
using NHibernate.ByteCode.LinFu;
using NHibernate.Tool.hbm2ddl;
using NServiceBus.Config;
using NServiceBus.ObjectBuilder;
using NServiceBus.Unicast.Subscriptions.NHibernate;
using NServiceBus.Unicast.Subscriptions.NHibernate.Config;
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
            var nhibernateProperties = SQLiteConfiguration
                .Standard
                .ProxyFactoryFactory(typeof(ProxyFactoryFactory).AssemblyQualifiedName)
                .UsingFile(".\\NServiceBus.Subscriptions.sqlite")
                .ToProperties();

            return DBSubcriptionStorage(config, nhibernateProperties, true);
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
                configSection.NHibernateProperties.ToProperties(),
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
            IDictionary<string, string> nhibernateProperties,
            bool autoUpdateSchema)
        {

            var fluentConfiguration = Fluently.Configure(new Configuration().SetProperties(nhibernateProperties))
              .Mappings(m => m.FluentMappings.Add(typeof(SubscriptionMap)));

            var cfg = fluentConfiguration.BuildConfiguration();

            if (autoUpdateSchema)
                new SchemaUpdate(cfg).Execute(false, true);

            //default to LinFu if not specifed by user
            if (!cfg.Properties.Keys.Contains(PROXY_FACTORY_KEY))
                fluentConfiguration.ExposeConfiguration(
                    x =>
                    x.SetProperty(PROXY_FACTORY_KEY, typeof(ProxyFactoryFactory).AssemblyQualifiedName));

            var sessionSource = new SessionSource(fluentConfiguration);


            config.Configurer.RegisterSingleton<ISessionSource>(sessionSource);
            config.Configurer.ConfigureComponent<SubscriptionStorage>(ComponentCallModelEnum.Singlecall);

            return config;

        }

        private const string PROXY_FACTORY_KEY = "proxyfactory.factory_class";
    }
}