using System;
using System.Collections.Generic;
using NHibernate;
using NServiceBus.Config;
using NServiceBus.SagaPersisters.NHibernate;
using NServiceBus.SagaPersisters.NHibernate.Config.Internal;

namespace NServiceBus
{
    using Common.Logging;

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
            IDictionary<string, string> nhibernateProperties = null;
            bool updateSchema = false;

            var configSection = Configure.GetConfigSection<NHibernateSagaPersisterConfig>();

            if (configSection != null)
            {
                nhibernateProperties = configSection.NHibernateProperties.ToProperties();
                updateSchema = configSection.UpdateSchema;
            }

            return NHibernateSagaPersister(config, nhibernateProperties, updateSchema);
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
            var nhibernateProperties = SQLiteConfiguration.UsingFile(".\\NServiceBus.Sagas.sqlite");

            AppDomain.CurrentDomain.UnhandledException += (o, e) =>
            {
                if (e.ExceptionObject.GetType() ==
                    typeof(AccessViolationException))
                    LogManager.GetLogger("System.Data.SQLite").Fatal(
                        "NServiceBus has detected an error in the operation of SQLite. SQLite is the database used to store sagas from NServiceBus when running under the 'Integration' profile. This error usually occurs only under load. If you wish to use sagas under load, it is recommended to run NServiceBus under the 'Production' profile. This can be done by passing the value 'NServiceBus.Production' on the command line to the NServiceBus.Host.exe process. For more information see http://particular.net/articles/profiles-for-nservicebus-host");
            };
            
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

            if (nhibernateProperties == null)
                throw new InvalidOperationException("No properties configured for NHibernate. Check that you have a configuration section called 'NHibernateSagaPersisterConfig'.");

            var builder = new SessionFactoryBuilder(Configure.TypesToScan);

            var sessionFactory = builder.Build(nhibernateProperties, autoUpdateSchema);

            if (sessionFactory == null)
                throw new InvalidOperationException("Could not create session factory for saga persistence.");

            config.Configurer.RegisterSingleton<ISessionFactory>(sessionFactory);
            config.Configurer.ConfigureComponent<SagaPersister>(DependencyLifecycle.InstancePerCall);
            config.NHibernateUnitOfWork();

            return config;
        }
    }
}
