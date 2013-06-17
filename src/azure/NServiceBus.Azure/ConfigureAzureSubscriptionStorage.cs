namespace NServiceBus
{
    using Config;
    using Microsoft.WindowsAzure.Storage;
    using Unicast.Subscriptions;

    /// <summary>
    /// Configuration extensions for the subscription storage
    /// </summary>
    public static class ConfigureAzureSubscriptionStorage
    {
        /// <summary>
        /// Configures NHibernate Azure Subscription Storage , Settings etc are read from custom config section
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        [ObsoleteEx(RemoveInVersion = "5.0", TreatAsErrorFromVersion = "4.0", Replacement = "AzureSubscriptionStorage")]        
        public static Configure AzureSubcriptionStorage(this Configure config)
        {
            return AzureSubscriptionStorage(config);
        }

        /// <summary>
        /// Configures NHibernate Azure Subscription Storage , Settings etc are read from custom config section
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public static Configure AzureSubscriptionStorage(this Configure config)
        {
            var configSection = Configure.GetConfigSection<AzureSubscriptionStorageConfig>();
            if (configSection == null) { return config; }

            return config.AzureSubcriptionStorage(configSection.ConnectionString, configSection.CreateSchema, configSection.TableName);
        }

        /// <summary>
        /// Configures the storage with the user supplied persistence configuration
        /// Azure tables are created if requested by the user
        /// </summary>
        /// <param name="config"></param>
        /// <param name="connectionString"></param>
        /// <param name="createSchema"></param>
        /// <param name="tableName"> </param>
        /// <returns></returns>
        [ObsoleteEx(RemoveInVersion = "5.0", TreatAsErrorFromVersion = "4.0", Replacement = "AzureSubscriptionStorage")]
        public static Configure AzureSubcriptionStorage(this Configure config,
                                                        string connectionString,
                                                        bool createSchema,
                                                        string tableName)
        {
            return config.AzureSubscriptionStorage(connectionString, createSchema, tableName);
        }

        /// <summary>
        /// Configures the storage with the user supplied persistence configuration
        /// Azure tables are created if requested by the user
        /// </summary>
        /// <param name="config"></param>
        /// <param name="connectionString"></param>
        /// <param name="createSchema"></param>
        /// <param name="tableName"> </param>
        /// <returns></returns>
        public static Configure AzureSubscriptionStorage(this Configure config,
            string connectionString,
            bool createSchema, 
            string tableName)
        {
            SubscriptionServiceContext.SubscriptionTableName = tableName;
            SubscriptionServiceContext.CreateIfNotExist = createSchema;

            var account = CloudStorageAccount.Parse(connectionString);
            SubscriptionServiceContext.Init(account.CreateCloudTableClient());

            config.Configurer.ConfigureComponent(() => new AzureSubscriptionStorage(account), DependencyLifecycle.InstancePerCall);

            return config;

        }        
    }
}
