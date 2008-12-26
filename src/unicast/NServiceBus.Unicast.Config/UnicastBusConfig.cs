using System.Configuration;

namespace NServiceBus.Config
{
    /// <summary>
    /// A configuration section for UnicastBus specific settings.
    /// </summary>
    public class UnicastBusConfig : ConfigurationSection
    {
        /// <summary>
        /// Gets/sets the address for sending control messages to the distributor.
        /// </summary>
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

        /// <summary>
        /// Gets/sets the distributor's data address - used as the return address of messages sent by this endpoint.
        /// </summary>
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

        /// <summary>
        /// Contains the mappings from message types (or groups of them) to endpoints.
        /// </summary>
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
