using System;
using NHibernate;
using NHibernate.Drivers.Azure.TableStorage;
using NServiceBus.Config;
using NServiceBus.ObjectBuilder;
using NServiceBus.SagaPersisters.NHibernate;
using NServiceBus.SagaPersisters.Azure.Config.Internal;

namespace NServiceBus
{
    /// <summary>
    /// Contains extension methods to NServiceBus.Configure for the NHibernate saga persister on top of Azure table storage.
    /// </summary>
    public static class ConfigureAzureSagaPersister
    {
        /// <summary>
        /// Use the NHibernate backed saga persister implementation.
        /// Be aware that this implementation deletes sagas that complete so as not to have the database fill up.
        /// SagaData classes are automatically mapped using Fluent NHibernate Conventions.
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public static Configure AzureSagaPersister(this Configure config)
        {
            string connectionstring = string.Empty;
            bool updateSchema = false;

            var configSection = Configure.GetConfigSection<AzureSagaPersisterConfig>();

            if (configSection != null)
            {
                connectionstring = configSection.ConnectionString;
                updateSchema = configSection.CreateSchema;
            }

            return AzureSagaPersister(config, connectionstring, updateSchema);
        }

        /// <summary>
        /// Use the NHibernate backed saga persister implementation on top of Azure table storage.
        /// SagaData classes are automatically mapped using Fluent NHibernate conventions
        /// and there persistence schema is automatically generated if requested.
        /// </summary>
        /// <param name="config"></param>
        /// <param name="connectionString"></param>
        /// <param name="autoUpdateSchema"></param>
        /// <returns></returns>
        public static Configure AzureSagaPersister(this Configure config,
            string connectionString,
            bool autoUpdateSchema)
        {
            if (!Sagas.Impl.Configure.SagasWereFound)
                return config; //no sagas - don't need to do anything

            var nhibernateProperties = MsSqlConfiguration.Azure(connectionString);

            var builder = new SessionFactoryBuilder(Configure.TypesToScan);

            var sessionFactory = builder.Build(nhibernateProperties, autoUpdateSchema);

            if (sessionFactory == null)
                throw new InvalidOperationException("Could not create session factory for saga persistence.");

            config.Configurer.RegisterSingleton<ISessionFactory>(sessionFactory);
            config.Configurer.ConfigureComponent<SagaPersister>(DependencyLifecycle.InstancePerCall);

            return config;
        }
    }
}
