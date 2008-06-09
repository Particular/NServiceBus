using System.Collections.Generic;
using System.Configuration;

namespace NServiceBus.Unicast.Config
{
    public class UnicastBusConfig : ConfigurationSection
    {
        [ConfigurationProperty("DistributorControlAddress", IsRequired = false)]
        public string DistributorControlAddress
        {
            get
            {
                string result = this["DistributorControlAddress"] as string;
                if (result != null && result.Length == 0)
                    result = null;

                return result;
            }
            set
            {
                this["DistributorControlAddress"] = value;
            }
        }

        [ConfigurationProperty("DistributorDataAddress", IsRequired = false)]
        public string DistributorDataAddress
        {
            get
            {
                string result = this["DistributorDataAddress"] as string;
                if (result != null && result.Length == 0)
                    result = null;

                return result;
            }
            set
            {
                this["DistributorDataAddress"] = value;
            }
        }

        [ConfigurationProperty("MessageEndpointMappings", IsRequired = false)]
        public MessageEndpointMappingCollection MessageEndpointMappings
        {
            get
            {
                return this["MessageEndpointMappings"] as MessageEndpointMappingCollection;
            }
            set
            {
                this["MessageEndpointMappings"] = value;
            }
        }
    }
}
