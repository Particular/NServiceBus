using System.Collections.Generic;
using FluentNHibernate.Cfg.Db;
using NHibernate;
using NHibernate.ByteCode.LinFu;
using NServiceBus.Config;
using NServiceBus.ObjectBuilder;
using NServiceBus.SagaPersisters.NHibernate;
using NServiceBus.SagaPersisters.NHibernate.Config;
using NServiceBus.SagaPersisters.NHibernate.Config.Internal;

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
        /// SagaData classes are automatically mapped using Fluent NHibernate Conventions.
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public static Configure NHibernateSagaPersister(this Configure config)
        {
            if (!Sagas.Impl.Configure.SagasWereFound)
                return config; //no sagas - don't need to do anything

            var builder = new SessionFactoryBuilder(Configure.TypesToScan);
            var configSection = Configure.GetConfigSection<NHibernateSagaPersisterConfig>();

            IDictionary<string, string> nhibernateProperties;
            bool updateSchema = true;

            if (configSection == null)
            {
                nhibernateProperties = SQLiteConfiguration.Standard
                    .UsingFile(".\\NServiceBus.Sagas.sqlite")
                    .ProxyFactoryFactory(typeof(ProxyFactoryFactory).AssemblyQualifiedName)
                    .ToProperties();
            }
            else
            {
                nhibernateProperties = configSection.NHibernateProperties.ToProperties();
            }

            config.Configurer.RegisterSingleton<ISessionFactory>(builder.Build(nhibernateProperties, updateSchema));

            return config;
        }

    }
}
