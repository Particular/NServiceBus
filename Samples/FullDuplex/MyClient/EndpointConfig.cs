using NServiceBus;

namespace MyClient
{
    public class EndpointConfig : IConfigureThisEndpoint, AsA_Client {}

    public class ClientInit : IWantCustomInitialization
    {
        public void Init()
        {
            Configure.Instance
                .RijndaelEncryptionService()
                .UnicastBus().DoNotAutoSubscribe();
        }
    }
}