namespace NServiceBus.Config
{
    using System;
    using System.Configuration;

    /// <summary>
    /// A configuration section for UnicastBus specific settings.
    /// </summary>
    [ObsoleteEx(
        Message = "Use of the application configuration file to configure NServiceBus is discouraged. Use the code first API instead.",
        RemoveInVersion = "7.0")]
    public partial class UnicastBusConfig : ConfigurationSection
    {
        /// <summary>
        /// Gets/sets the time to be received set on forwarded messages.
        /// </summary>
        [ConfigurationProperty("TimeToBeReceivedOnForwardedMessages", IsRequired = false)]
        [ObsoleteEx(
            Message = "Use of the application configuration file to configure TimeToBeReceived in forwarded messages is discouraged",
            RemoveInVersion = "7.0")]
        public TimeSpan TimeToBeReceivedOnForwardedMessages
        {
            get { return (TimeSpan) this["TimeToBeReceivedOnForwardedMessages"]; }
            set { this["TimeToBeReceivedOnForwardedMessages"] = value; }
        }

        /// <summary>
        /// Gets/sets the address that the timeout manager will use to send and receive messages.
        /// </summary>
        [ConfigurationProperty("TimeoutManagerAddress", IsRequired = false)]
        [ObsoleteEx(
            Message = "Use of the application configuration file to configure an external TimeoutManager address is discouraged",
            ReplacementTypeOrMember = "EndpointConfiguration.UseExternalTimeoutManager",
            RemoveInVersion = "7.0")]
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
        [ObsoleteEx(
            Message = "Use of the application configuration file to configure routing is discouraged",
            ReplacementTypeOrMember = "EndpointConfiguration.UseTransport<T>.Routing()",
            RemoveInVersion = "7.0")]
        public MessageEndpointMappingCollection MessageEndpointMappings
        {
            get { return this["MessageEndpointMappings"] as MessageEndpointMappingCollection; }
            set { this["MessageEndpointMappings"] = value; }
        }
    }
}