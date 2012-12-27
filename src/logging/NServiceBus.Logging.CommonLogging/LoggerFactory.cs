using System;
using CommonLoggingManager = Common.Logging.LogManager;

namespace NServiceBus.Logging.CommonLogging
{
    public class LoggerFactory: ILoggerFactory
    {

        public ILog GetLogger(Type type)
        {
            return GetLogger(type.FullName);
        }

        public ILog GetLogger(string name)
        {
            var logger = CommonLoggingManager.GetLogger(name);
            return new Log(logger);
        }
    }
}