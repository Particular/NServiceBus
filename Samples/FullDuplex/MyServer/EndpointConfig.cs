using NServiceBus;
using NServiceBus.Host;

namespace Server
{
    public class EndpointConfig : IConfigureThisEndpoint, AsA_Server {}

    // this can't be done in the EndpointConfig class above without the full NServiceBus.Configure.With() thing
    public class ServerInit : IWantCustomInitialization
    {
        public void Init()
        {
            Configure.Instance.RijndaelEncryptionService();
        }
    }
}