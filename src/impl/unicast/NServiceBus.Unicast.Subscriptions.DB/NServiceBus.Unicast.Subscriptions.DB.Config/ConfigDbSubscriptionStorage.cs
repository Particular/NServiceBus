using ObjectBuilder;
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
        public ConfigDbSubscriptionStorage() : base() { }

        /// <summary>
        /// Wraps the given configuration object but stores the same 
        /// builder and configurer properties.
        /// </summary>
        /// <param name="config"></param>
        public void Configure(Configure config)
        {
            this.Builder = config.Builder;
            this.Configurer = config.Configurer;

            this.storage = this.Configurer.ConfigureComponent<SubscriptionStorage>(ComponentCallModelEnum.Singleton);

            DbSubscriptionStorageConfig cfg = ConfigurationManager.GetSection("DbSubscriptionStorageConfig") as DbSubscriptionStorageConfig;

            if (cfg == null)
                throw new ConfigurationErrorsException("Could not find configuration section for DB Subscription Storage.");

            this.storage.ConnectionString = cfg.ConnectionString;
            this.storage.ProviderInvariantName = cfg.ProviderInvariantName;
        }

        private SubscriptionStorage storage;

        /// <summary>
        /// Sets the table in the database in which subscriptions will be stored.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public ConfigDbSubscriptionStorage Table(string value)
        {
            this.storage.Table = value;
            return this;
        }

        /// <summary>
        /// Sets the name of the column that will store the message type.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public ConfigDbSubscriptionStorage MessageTypeParameterName(string value)
        {
            this.storage.MessageTypeParameterName = value;
            return this;
        }

        /// <summary>
        /// Sets the name of the column that will store the subscriber endpoint.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public ConfigDbSubscriptionStorage SubscriberEndpointParameterName(string value)
        {
            this.storage.SubscriberParameterName = value;
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
            this.storage.IsolationLevel = value;
            return this;
        }
    }
}
