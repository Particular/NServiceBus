namespace NServiceBus.Config
{
    using System;
    using System.Configuration;

    /// <summary>
    /// A configuration section for UnicastBus specific settings.
    /// </summary>
    public partial class UnicastBusConfig : ConfigurationSection
    {
        /// <summary>
        /// Gets/sets the time to be received set on forwarded messages.
        /// </summary>
        [ConfigurationProperty("TimeToBeReceivedOnForwardedMessages", IsRequired = false)]
        public TimeSpan TimeToBeReceivedOnForwardedMessages
        {
            get { return (TimeSpan) this["TimeToBeReceivedOnForwardedMessages"]; }
            set { this["TimeToBeReceivedOnForwardedMessages"] = value; }
        }

        /// <summary>
        /// Gets/sets the address that the timeout manager will use to send and receive messages.
        /// </summary>
        [ConfigurationProperty("TimeoutManagerAddress", IsRequired = false)]
        public string TimeoutManagerAddress
        {
            get
            {
                var result = this["TimeoutManagerAddress"] as string;
                if (string.IsNullOrWhiteSpace(result))
                {
                    result = null;
                }

                return result;
            }
            set { this["TimeoutManagerAddress"] = value; }
        }

        /// <summary>
        /// Contains the mappings from message types (or groups of them) to endpoints.
        /// </summary>
        [ConfigurationProperty("MessageEndpointMappings", IsRequired = false)]
        public MessageEndpointMappingCollection MessageEndpointMappings
        {
            get { return this["MessageEndpointMappings"] as MessageEndpointMappingCollection; }
            set { this["MessageEndpointMappings"] = value; }
        }
    }
}