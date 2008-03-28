using System.Configuration;

namespace NServiceBus.Unicast.Config
{
    public class MessageEndpointMapping : ConfigurationElement
    {
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
