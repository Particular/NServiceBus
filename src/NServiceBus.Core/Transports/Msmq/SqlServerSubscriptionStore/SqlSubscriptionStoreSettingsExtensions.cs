namespace NServiceBus
{
    using NServiceBus.Configuration.AdvanceExtensibility;
    using NServiceBus.Transports.Msmq;

    /// <summary>
    /// Allows to customize settings of SQL Server subscription store.
    /// </summary>
    public static class SqlSubscriptionStoreSettingsExtensions
    {
        /// <summary>
        /// Overrides the connection string for subscription store. 
        /// </summary>
        /// <param name="definiton">Store definition.</param>
        /// <param name="connectionString">Connection string.</param>
        public static SubscriptionStoreSettings<SqlServerSubscriptionStore> ConnectionString(this SubscriptionStoreSettings<SqlServerSubscriptionStore> definiton, string connectionString)
        {
            Guard.AgainstNullAndEmpty(nameof(connectionString), connectionString);
            definiton.GetSettings().Set(SettingsKeys.SubscriptionStoreConnectionStringKey, connectionString);
            return definiton;
        }

        /// <summary>
        /// Overrides the schema for subscription store (defaults to "dbo").
        /// </summary>
        /// <param name="definiton">Store definition.</param>
        /// <param name="schemaName">Schema.</param>
        public static SubscriptionStoreSettings<SqlServerSubscriptionStore> Schema(this SubscriptionStoreSettings<SqlServerSubscriptionStore> definiton, string schemaName)
        {
            Guard.AgainstNullAndEmpty(nameof(schemaName), schemaName);
            definiton.GetSettings().Set(SettingsKeys.SubscriptionStoreSchemaKey, schemaName);
            return definiton;
        }

        /// <summary>
        /// Overrides the table name for subscription store (defaults to "Subscriptions").
        /// </summary>
        /// <param name="definiton">Store definition.</param>
        /// <param name="tableName">Table name.</param>
        public static SubscriptionStoreSettings<SqlServerSubscriptionStore> Table(this SubscriptionStoreSettings<SqlServerSubscriptionStore> definiton, string tableName)
        {
            Guard.AgainstNullAndEmpty(nameof(tableName), tableName);
            definiton.GetSettings().Set(SettingsKeys.SubscriptionStoreTableKey, tableName);
            return definiton;
        }
    }
}