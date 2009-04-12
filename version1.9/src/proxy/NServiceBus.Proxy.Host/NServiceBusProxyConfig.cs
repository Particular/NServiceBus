using System.Configuration;

namespace NServiceBus.Proxy.Host
{
    public class NServiceBusProxyConfig : ConfigurationSection
    {
        [ConfigurationProperty("RemoteServer", IsRequired = true)]
        public string RemoteServer
        {
            get
            {
                return this["RemoteServer"] as string;
            }
            set
            {
                this["RemoteServer"] = value;
            }
        }
    }
}
