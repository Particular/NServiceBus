using NServiceBus;

namespace TestServer
{
    public class EndpointConfig : AsA_Server, IConfigureThisEndpoint, IWantCustomInitialization
    {
        public void Init()
        {
            Configure.With()
                .DefaultBuilder()
                .FtpTransport()
                .UnicastBus()
                    .LoadMessageHandlers();
        }
    }
}
