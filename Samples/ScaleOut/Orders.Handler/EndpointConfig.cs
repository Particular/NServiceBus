using NServiceBus;

namespace Orders.Handler
{
    public class EndpointConfig : IConfigureThisEndpoint, AsA_Publisher, IWantCustomLogging
    {
        public void Init()
        {
            NServiceBus.SetLoggingLibrary.Log4Net(log4net.Config.XmlConfigurator.Configure);
        }
    }
}
