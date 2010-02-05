using NServiceBus;
using log4net;

namespace LoggingFromAppConfig
{
    class Logger : IWantToRunAtStartup
    {
        public void Run()
        {
            Log.Debug("logging this.");
        }

        public void Stop()
        {
        }

        private static readonly ILog Log = LogManager.GetLogger("Logger");
    }
}
