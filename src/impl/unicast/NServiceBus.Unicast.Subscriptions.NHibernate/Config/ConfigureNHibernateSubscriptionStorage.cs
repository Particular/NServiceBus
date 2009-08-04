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
using Configuration=NHibernate.Cfg.Configuration;

namespace NServiceBus
{
    /// <summary>
    /// Configuration extensions for the NHibernate subscription storage
    /// </summary>
    public static class ConfigureNHibernateSubscriptionStorage
    {

        /// <summary>
        /// Configures the storage with the user supplied persistence configuration
        /// DB schema is updated if requested by the user
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public static Configure DBSubcriptionStorage(this Configure config)
        {
            IDictionary<string, string> nhibernateProperties;
            bool updateSchema = true;

            var configSection = Configure.GetConfigSection<DBSubscriptionStorageConfig>();

            if (configSection == null || configSection.NHibernateProperties.Count == 0 )
            {
                nhibernateProperties = SQLiteConfiguration
                    .Standard
                    .ProxyFactoryFactory(typeof(ProxyFactoryFactory).AssemblyQualifiedName)
                    .UsingFile(".\\NServiceBus.Subscriptions.sqlite")
                    .ToProperties();
            }
            else
            {
                nhibernateProperties = configSection.NHibernateProperties.ToProperties();
                updateSchema = configSection.UpdateSchema;
            }

            var fluentConfiguration = Fluently.Configure(new Configuration().SetProperties(nhibernateProperties))
              .Mappings(m => m.FluentMappings.AddFromAssemblyOf<Subscription>());

            var cfg = fluentConfiguration.BuildConfiguration();

            if (updateSchema)
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