using System.Configuration;

namespace NServiceBus.Config
{
    /// <summary>
    /// A configuration element representing which message types map to which endpoint.
    /// </summary>
    public class MessageEndpointMapping : ConfigurationElement
    {
        /// <summary>
        /// A string defining the message assembly, or single message type.
        /// </summary>
        [ConfigurationProperty("Messages", IsRequired = true, IsKey = true)]
        public string Messages
        {
            get
            {
                return (string)this["Messages"];
            }
            set
            {
                this["Messages"] = value;
            }
        }

        /// <summary>
        /// The endpoint named according to "queue@machine".
        /// </summary>
        [ConfigurationProperty("Endpoint", IsRequired = true, IsKey = true)]
        public string Endpoint
        {
            get
            {
                return (string)this["Endpoint"];
            }
            set
            {
                this["Endpoint"] = value;
            }
        }
    }
}
