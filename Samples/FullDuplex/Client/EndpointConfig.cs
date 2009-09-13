using NServiceBus;
using NServiceBus.Host;

namespace Client
{
    public class EndpointConfig : IConfigureThisEndpoint, AsA_Client,
        IWantCustomInitialization
    {
        public void Init()
        {
            Configure.Instance.RijndaelEncryptionService();
        }
    }
}