using NServiceBus;

namespace MyServer
{
    public class EndpointConfig : IConfigureThisEndpoint, AsA_Server {}

    // This demonstrates the kind of extensible initialization that NServiceBus supports.
    // You can implement as many IWantCustomInitialization classes as you need in as many places as you want
    public class ServerInit : IWantCustomInitialization
    {
        public void Init()
        {
            Configure.Instance
                .RijndaelEncryptionService();
        }
    }
}