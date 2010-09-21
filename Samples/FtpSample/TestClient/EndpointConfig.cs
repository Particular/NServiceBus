using NServiceBus;

namespace TestClient
{
    public class EndpointConfig : IWantCustomInitialization, IConfigureThisEndpoint
    {
        #region IWantCustomInitialization Members

        public void Init()
        {
            Configure.With()
                .DefaultBuilder()                
                .FtpTransport()
                .UnicastBus()
                    .LoadMessageHandlers();
            
            
                
        }

        #endregion
    }
}
