using NServiceBus;
using NServiceBus.Host;

namespace Server
{
    public class EndpointConfig : IConfigureThisEndpoint, AsA_Server {}

    public class ServerInit : IWantCustomInitialization
    {
        public void Init()
        {
            Configure.Instance.RijndaelEncryptionService();
        }
    }
}