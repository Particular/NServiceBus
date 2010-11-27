using System;
using log4net;
using NServiceBus;

namespace LoggingWithConfigurableThreshold
{
    public class EndpointConfig : IConfigureThisEndpoint, IWantToRunAtStartup
    {
        public void Run()
        {
            Console.WriteLine("The WARN threshold has been set in the config file.");

            LogManager.GetLogger("A").Debug("This should not appear");
            LogManager.GetLogger("A").Warn("This should appear");
        }

        public void Stop()
        {
        }
    }
}
