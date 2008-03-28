using System.Configuration;

namespace DbBlobSagaPersister.Config
{
    public class SagaPersisterConfig : ConfigurationSection
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
