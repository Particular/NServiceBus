using ObjectBuilder;
using System.Configuration;
using System.Data;

namespace NServiceBus.Unicast.Subscriptions.DB.Config
{
    public class ConfigDbSubscriptionStorage
    {
        public ConfigDbSubscriptionStorage(IBuilder builder)
        {
            this.storage = builder.ConfigureComponent<SubscriptionStorage>(ComponentCallModelEnum.Singleton);

            DbSubscriptionStorageConfig cfg = ConfigurationManager.GetSection("DbSubscriptionStorageConfig") as DbSubscriptionStorageConfig;

            if (cfg == null)
                throw new ConfigurationErrorsException("Could not find configuration section for DB Subscription Storage.");

            this.storage.ConnectionString = cfg.ConnectionString;
            this.storage.ProviderInvariantName = cfg.ProviderInvariantName;
        }

        private SubscriptionStorage storage;

        public ConfigDbSubscriptionStorage Table(string value)
        {
            this.storage.Table = value;
            return this;
        }

        public ConfigDbSubscriptionStorage MessageTypeParameterName(string value)
        {
            this.storage.MessageTypeParameterName = value;
            return this;
        }

        public ConfigDbSubscriptionStorage SubscriberEndpointParameterName(string value)
        {
            this.storage.SubscriberParameterName = value;
            return this;
        }

        /// <summary>
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
