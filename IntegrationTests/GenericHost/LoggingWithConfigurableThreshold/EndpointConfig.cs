using System;
using NServiceBus;
using NServiceBus.Logging;

namespace LoggingWithConfigurableThreshold
{
    public class EndpointConfig : IConfigureThisEndpoint, AsA_Server, IWantToRunWhenBusStartsAndStops
    {
        static ILog logger = LogManager.GetLogger("A");

        public void Start()
        {
            Console.WriteLine("The WARN threshold has been set in the config file.");

            logger.Debug("This should not appear");
            logger.Warn("This should appear");
        }

        public void Stop()
        {
        }

        public void Customize(BusConfiguration configuration)
        {
            configuration.UsePersistence<InMemoryPersistence>();
        }
    }
}
