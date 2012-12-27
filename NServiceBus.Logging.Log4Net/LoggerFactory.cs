using System;
using log4netManager = log4net.LogManager;

namespace NServiceBus.Logging.Log4Net
{
    public class LoggerFactory: ILoggerFactory
    {

        public ILog GetLogger(Type type)
        {
            return GetLogger(type.FullName);
        }

        public ILog GetLogger(string name)
        {
            var logger = log4netManager.GetLogger(name);
            return new Log(logger);
        }
    }
}