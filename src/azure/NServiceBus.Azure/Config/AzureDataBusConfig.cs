using System.Configuration;

namespace NServiceBus.Config
{
    public class AzureDataBusConfig : ConfigurationSection
    {
        [ConfigurationProperty("MaxRetries", IsRequired = false, DefaultValue = ConfigureAzureBlobStorageDataBus.DefaultMaxRetries)]
        public int MaxRetries
        {
            get
            {
                return (int)this["MaxRetries"];
            }
            set
            {
                this["MaxRetries"] = value;
            }
        }

        [ConfigurationProperty("BlockSize", IsRequired = false, DefaultValue = ConfigureAzureBlobStorageDataBus.DefaultBlockSize)]
        public int BlockSize
        {
            get
            {
                return (int)this["BlockSize"];
            }
            set
            {
                this["BlockSize"] = value;
            }
        }


        [ConfigurationProperty("NumberOfIOThreads", IsRequired = false, DefaultValue = ConfigureAzureBlobStorageDataBus.DefaultNumberOfIOThreads)]
        public int NumberOfIOThreads
        {
            get
            {
                return (int)this["NumberOfIOThreads"];
            }
            set
            {
                this["NumberOfIOThreads"] = value;
            }
        }

        [ConfigurationProperty("ConnectionString", IsRequired = false, DefaultValue = ConfigureAzureBlobStorageDataBus.DefaultConnectionString)]
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

        [ConfigurationProperty("Container", IsRequired = false, DefaultValue = ConfigureAzureBlobStorageDataBus.Defaultcontainer)]
        public string Container
        {
            get
            {
                return (string)this["Container"];
            }
            set
            {
                this["Container"] = value;
            }
        }

        [ConfigurationProperty("BasePath", IsRequired = false, DefaultValue = ConfigureAzureBlobStorageDataBus.DefaultBasePath)]
        public string BasePath
        {
            get
            {
                return (string)this["BasePath"];
            }
            set
            {
                this["BasePath"] = value;
            }
        }
    }
}