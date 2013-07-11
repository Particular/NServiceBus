using System;
using NServiceBus.Config;

namespace NServiceBus
{
    using Microsoft.WindowsAzure.Storage;
    using SagaPersisters.Azure;
    
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
            var account = CloudStorageAccount.Parse(connectionString);

            config.Configurer.ConfigureComponent(() => new AzureSagaPersister(account, autoUpdateSchema), DependencyLifecycle.InstancePerCall);

            return config;
        }
    }
}
