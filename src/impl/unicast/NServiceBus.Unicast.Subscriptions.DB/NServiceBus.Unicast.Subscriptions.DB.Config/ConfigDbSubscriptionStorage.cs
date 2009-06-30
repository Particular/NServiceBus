using NServiceBus.ObjectBuilder;
using System.Configuration;
using System.Data;
using NServiceBus.Config;

namespace NServiceBus.Unicast.Subscriptions.DB.Config
{
    /// <summary>
    /// Extends the base Configure class with DbSubscriptionStorage specific methods.
    /// Reads administrator set values from the DbSubscriptionStorageConfig section
    /// of the app.config.
    /// </summary>
    public class ConfigDbSubscriptionStorage : Configure
    {
        /// <summary>
        /// Wraps the given configuration object but stores the same 
        /// builder and configurer properties.
        /// </summary>
        /// <param name="config"></param>
        public void Configure(Configure config)
        {
            Builder = config.Builder;
            Configurer = config.Configurer;

            storageConfig = Configurer.ConfigureComponent<SubscriptionStorage>(ComponentCallModelEnum.Singleton);

            var cfg = GetConfigSection<DbSubscriptionStorageConfig>();

            if (cfg == null)
                throw new ConfigurationErrorsException("Could not find configuration section for DB Subscription Storage.");

            storageConfig.ConfigureProperty(s => s.ConnectionString, cfg.ConnectionString);
            storageConfig.ConfigureProperty(s => s.ProviderInvariantName, cfg.ProviderInvariantName);
        }

        private IComponentConfig<SubscriptionStorage> storageConfig;

        /// <summary>
        /// Sets the table in the database in which subscriptions will be stored.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public ConfigDbSubscriptionStorage Table(string value)
        {
            storageConfig.ConfigureProperty(s => s.Table, value);
            return this;
        }

        /// <summary>
        /// Sets the name of the column that will store the message type.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public ConfigDbSubscriptionStorage MessageTypeColumnName(string value)
        {
            storageConfig.ConfigureProperty(s => s.MessageTypeColumnName, value);
            return this;
        }

        /// <summary>
        /// Sets the name of the column that will store the subscriber endpoint.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public ConfigDbSubscriptionStorage SubscriberEndpointColumnName(string value)
        {
            storageConfig.ConfigureProperty(s => s.SubscriberColumnName, value);
            return this;
        }

        /// <summary>
        /// Sets the isolation level of transactions used to write to the database
        /// for the purpose of managing subscription data.
        /// 
        /// Default level is ReadCommitted.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public ConfigDbSubscriptionStorage IsolationLevel(IsolationLevel value)
        {
            storageConfig.ConfigureProperty(s => s.IsolationLevel, value);
            return this;
        }
    }
}
