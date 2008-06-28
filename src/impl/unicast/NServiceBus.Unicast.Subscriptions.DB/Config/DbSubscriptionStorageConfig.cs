using System.Configuration;

namespace NServiceBus.Unicast.Subscriptions.DB.Config
{
    public class DbSubscriptionStorageConfig : ConfigurationSection
    {
        [ConfigurationProperty("ProviderInvariantName", IsRequired = true)]
        public string ProviderInvariantName
        {
            get
            {
                return this["ProviderInvariantName"] as string;
            }
            set
            {
                this["ProviderInvariantName"] = value;
            }
        }

        [ConfigurationProperty("ConnectionString", IsRequired = true)]
        public string ConnectionString
        {
            get
            {
                return this["ConnectionString"] as string;
            }
            set
            {
                this["ConnectionString"] = value;
            }
        }

    }
}
