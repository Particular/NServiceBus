namespace NServiceBus
{
    using System;
    using NServiceBus.Settings;
    using NServiceBus.Transports.Msmq;

    /// <summary>
    /// SQL Server subscription store for MSMQ.
    /// </summary>
    public class SqlServerSubscriptionStore : SubscriptionStoreDefinition
    {
        /// <summary>
        /// Initializes the definition.
        /// </summary>
        /// <param name="settings">Settings.</param>
        /// <returns>Subscription store infrastructure.</returns>
        protected internal override SubscriptionStoreInfrastructure Initialize(SettingsHolder settings)
        {
            settings.SetDefault(SettingsKeys.SubscriptionStoreSchemaKey, "dbo");
            settings.SetDefault(SettingsKeys.SubscriptionStoreTableKey, "Subscriptions");

            var schema = settings.Get<string>(SettingsKeys.SubscriptionStoreSchemaKey);
            var table = settings.Get<string>(SettingsKeys.SubscriptionStoreTableKey);
            string connectionString;
            if (!settings.TryGet(SettingsKeys.SubscriptionStoreConnectionStringKey, out connectionString))
            {
                throw new Exception("Please specify the connection string to use for SQL Server subscription store.");
            }
            return new SubscriptionStoreInfrastructure(
                () => new SqlServerSubscriptionReader(schema, table, connectionString),
                () => new SqlServerSubscriptionManager(settings.EndpointName().ToString(), settings.LocalAddress(), schema, table, connectionString));
        }
    }
}