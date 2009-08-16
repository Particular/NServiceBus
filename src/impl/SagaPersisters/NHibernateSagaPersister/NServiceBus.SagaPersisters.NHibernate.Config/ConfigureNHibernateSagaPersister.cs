using System;
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

            if (configSection == null)
                throw new InvalidOperationException("Configuration section 'NHibernateSagaPersisterConfig' could not be found.");

            var nhibernateProperties = configSection.NHibernateProperties.ToProperties();

            var sessionFactory = builder.Build(nhibernateProperties, false);

            if (sessionFactory == null)
                throw new InvalidOperationException("Could not create session factory for saga persistence.");

            config.Configurer.RegisterSingleton<ISessionFactory>(sessionFactory);
            config.Configurer.ConfigureComponent<SagaPersister>(ComponentCallModelEnum.Singlecall);

            return config;
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
            if (!Sagas.Impl.Configure.SagasWereFound)
                return config; //no sagas - don't need to do anything

            var builder = new SessionFactoryBuilder(Configure.TypesToScan);

            var nhibernateProperties = SQLiteConfiguration.Standard
                    .UsingFile(".\\NServiceBus.Sagas.sqlite")
                    .ProxyFactoryFactory(typeof(ProxyFactoryFactory).AssemblyQualifiedName)
                    .ToProperties();

            var sessionFactory = builder.Build(nhibernateProperties, true);

            if (sessionFactory == null)
                throw new InvalidOperationException("Could not create session factory for saga persistence.");

            config.Configurer.RegisterSingleton<ISessionFactory>(sessionFactory);
            config.Configurer.ConfigureComponent<SagaPersister>(ComponentCallModelEnum.Singlecall);

            return config;
        }

    }
}
