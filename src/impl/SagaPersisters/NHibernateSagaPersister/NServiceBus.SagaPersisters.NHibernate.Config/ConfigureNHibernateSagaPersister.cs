using System;
using System.Collections.Generic;
using FluentNHibernate.Cfg.Db;
using NHibernate;
using NHibernate.ByteCode.LinFu;
using NServiceBus.Config;
using NServiceBus.ObjectBuilder;
using NServiceBus.SagaPersisters.NHibernate;
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
            var configSection = Configure.GetConfigSection<NHibernateSagaPersisterConfig>();

            if (configSection == null)
                throw new InvalidOperationException("Configuration section 'NHibernateSagaPersisterConfig' could not be found. If you don't want sagas for this endpoint, please implement IDontWant.Sagas on your IConfigureThisEndpoint class.");
            
            var nhibernateProperties = configSection.NHibernateProperties.ToProperties();

            return NHibernateSagaPersister(config, nhibernateProperties, configSection.UpdateSchema);
        }

        /// <summary>
        /// Use the NHibernate backed saga persister implementation on top of SQLite.
        /// SagaData classes are automatically mapped using Fluent NHibernate conventions
        /// and there persistence schema is also automatically generated.
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public static Configure NHibernateSagaPersisterWithSQLiteAndAutomaticSchemaGeneration(this Configure config)
        {
         
            var nhibernateProperties = SQLiteConfiguration.Standard
                    .UsingFile(".\\NServiceBus.Sagas.sqlite")
                    .ProxyFactoryFactory(typeof(ProxyFactoryFactory).AssemblyQualifiedName)
                    .ToProperties();

            return NHibernateSagaPersister(config, nhibernateProperties, true);
        }


        /// <summary>
        /// Use the NHibernate backed saga persister implementation.
        /// SagaData classes are automatically mapped using Fluent NHibernate conventions
        /// and there persistence schema is automatically generated if requested.
        /// </summary>
        /// <param name="config"></param>
        /// <param name="nhibernateProperties"></param>
        /// <param name="autoUpdateSchema"></param>
        /// <returns></returns>
        public static Configure NHibernateSagaPersister(this Configure config,
            IDictionary<string,string> nhibernateProperties,
            bool autoUpdateSchema)
        {
            if (!Sagas.Impl.Configure.SagasWereFound)
                return config; //no sagas - don't need to do anything

            var builder = new SessionFactoryBuilder(Configure.TypesToScan);

            var sessionFactory = builder.Build(nhibernateProperties, autoUpdateSchema);

            if (sessionFactory == null)
                throw new InvalidOperationException("Could not create session factory for saga persistence.");

            config.Configurer.RegisterSingleton<ISessionFactory>(sessionFactory);
            config.Configurer.ConfigureComponent<SagaPersister>(ComponentCallModelEnum.Singlecall);

            return config;
        }
    }
}
