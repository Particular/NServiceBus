using NServiceBus;
using log4net;

namespace LoggingFromAppConfig
{
    class Logger : IWantToRunWhenBusStartsAndStops
    {
        public void Start()
        {
            Log.Debug("logging this.");
        }

        public void Stop()
        {
        }

        private static readonly ILog Log = LogManager.GetLogger("Logger");
    }
}
