using System.Configuration;

namespace NServiceBus.Config
{
    public class AppFabricQueueConfig : ConfigurationSection
    {
        [ConfigurationProperty("IssuerName", IsRequired = true)]
        public string IssuerName
        {
            get
            {
                return (string)this["IssuerName"];
            }
            set
            {
                this["IssuerName"] = value;
            }
        }

        [ConfigurationProperty("IssuerKey", IsRequired = true)]
        public string IssuerKey
        {
            get
            {
                return (string)this["IssuerKey"];
            }
            set
            {
                this["IssuerKey"] = value;
            }
        }

        [ConfigurationProperty("ServiceNamespace", IsRequired = true)]
        public string ServiceNamespace
        {
            get
            {
                return (string)this["ServiceNamespace"];
            }
            set
            {
                this["ServiceNamespace"] = value;
            }
        }
   }
}