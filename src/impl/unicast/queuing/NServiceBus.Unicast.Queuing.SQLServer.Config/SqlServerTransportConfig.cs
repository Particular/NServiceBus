using System.Configuration;

namespace NServiceBus.Config
{
    public class SqlServerTransportConfig : ConfigurationSection
    {
        [ConfigurationProperty("ConnectionString", IsRequired = false)]
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