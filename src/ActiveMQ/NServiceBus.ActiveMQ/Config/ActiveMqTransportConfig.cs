namespace NServiceBus.Config
{
    using System.Configuration;

    public class ActiveMqTransportConfig : ConfigurationSection
    {
        [ConfigurationProperty("BrokerUri", IsRequired = true)]
        public string BrokerUri
        {
            get
            {
                return this["BrokerUri"] as string;
            }
            set
            {
                this["BrokerUri"] = value;
            }
        }
    }
}