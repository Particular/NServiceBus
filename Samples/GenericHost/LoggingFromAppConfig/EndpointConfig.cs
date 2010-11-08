using System;
using NServiceBus;

namespace LoggingFromAppConfig
{
    public class EndpointConfig : IConfigureThisEndpoint, AsA_Client, IWantCustomLogging
    {
        public void Init()
        {
            Console.WriteLine("I'm using the logging configured in the app.config.");
            NServiceBus.SetLoggingLibrary.Log4Net(log4net.Config.XmlConfigurator.Configure);
        }
    }
}
