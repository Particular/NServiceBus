using System;
using NServiceBus.Host;

namespace LoggingFromAppConfig
{
    public class EndpointConfig : IConfigureThisEndpoint, AsA_Client, IConfigureLogging
    {
        public void Configure(IConfigureThisEndpoint specifier)
        {
            Console.WriteLine("I'm using the logging configured in the app.config.");
        }
    }
}
