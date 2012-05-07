using NServiceBus;

namespace TestClient
{
    public class EndpointConfig : AsA_Client, IWantCustomInitialization, IConfigureThisEndpoint
    {
        public void Init()
        {
            Configure.With()
                .DefineEndpointName("localhost:1091")
                .DefaultBuilder()                
                .FtpTransport()
                .UnicastBus()
                    .LoadMessageHandlers();
        }
    }
}
