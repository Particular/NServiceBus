using System.Configuration;

namespace NServiceBus.Config
{
    public class AzureSubscriptionStorageConfig : ConfigurationSection
    {
        [ConfigurationProperty("ConnectionString", IsRequired = false, DefaultValue = "UseDevelopmentStorage=true")]
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

        [ConfigurationProperty("CreateSchema", IsRequired = false,DefaultValue = true)]
        public bool CreateSchema
        {
            get
            {

                return (bool)this["CreateSchema"];
            }
            set
            {
                this["CreateSchema"] = value;
            }
        }
    }
}