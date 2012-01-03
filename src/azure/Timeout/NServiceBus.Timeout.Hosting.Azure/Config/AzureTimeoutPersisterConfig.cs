using System.Configuration;

namespace NServiceBus.Timeout.Hosting.Azure
{
    public class AzureTimeoutPersisterConfig : ConfigurationSection
    {
        [ConfigurationProperty("ConnectionString", IsRequired = false, DefaultValue = "UseDevelopmentStorage=true")]
        public string ConnectionString
        {
            get { return (string)this["ConnectionString"]; }
            set { this["ConnectionString"] = value; }
        }
    }
}