using System;
using NServiceBus;

namespace TestServer
{
    public class EndpointConfig : IConfigureThisEndpoint, IWantCustomInitialization
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
