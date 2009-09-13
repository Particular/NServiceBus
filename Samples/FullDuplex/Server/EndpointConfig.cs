using NServiceBus;
using NServiceBus.Host;

namespace Server
{
    public class EndpointConfig : IConfigureThisEndpoint, AsA_Server,
        IWantCustomInitialization
    {
        public void Init()
        {
            Configure.Instance.RijndaelEncryptionService();
        }
    }
}