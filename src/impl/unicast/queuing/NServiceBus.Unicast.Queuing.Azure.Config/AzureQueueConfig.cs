using System.Configuration;

namespace NServiceBus.Config
{
    public class AzureQueueConfig : ConfigurationSection
    {
        [ConfigurationProperty("ConnectionString", IsRequired = true)]
        public string ConnectionString
        {
            get
            {
                return (string)this["ConnectionString"];
            }
            set
            {
                this["ConnectionString"] = value;
            }
        }
   }
}