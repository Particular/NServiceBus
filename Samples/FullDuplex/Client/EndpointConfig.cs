using NServiceBus;
using NServiceBus.Host;

namespace Client
{
    public class EndpointConfig : IConfigureThisEndpoint, AsA_Client {}

    public class ClientInit : IWantCustomInitialization
    {
        public void Init()
        {
            Configure.Instance.RijndaelEncryptionService();
        }
    }
}