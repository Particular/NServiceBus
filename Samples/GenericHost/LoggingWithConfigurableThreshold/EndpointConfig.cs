using System;
using log4net;
using log4net.Appender;
using NServiceBus;

namespace LoggingWithConfigurableThreshold
{
    public class EndpointConfig : IConfigureThisEndpoint, IWantCustomLogging, IWantToRunAtStartup
    {
        public void Init()
        {
            NServiceBus.SetLoggingLibrary.Log4Net<ConsoleAppender>(null, ca => ca.Name = "Udi");
            Console.WriteLine("The WARN threshold has been set in the config file.");
        }

        public void Run()
        {
            LogManager.GetLogger("A").Debug("This should not appear");
            LogManager.GetLogger("A").Warn("This should appear");
        }

        public void Stop()
        {
        }
    }
}
