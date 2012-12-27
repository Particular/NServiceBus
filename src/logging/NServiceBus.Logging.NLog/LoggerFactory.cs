using System;
using NLogManager = NLog.LogManager;

namespace NServiceBus.Logging.NLog
{
    public class LoggerFactory: ILoggerFactory
    {

        public ILog GetLogger(Type type)
        {
            return GetLogger(type.FullName);
        }

        public ILog GetLogger(string name)
        {
            var logger = NLogManager.GetLogger(name);
            return new Log(logger);
        }
    }
}