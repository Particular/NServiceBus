using System;

namespace NServiceBus.Logging.Loggers
{
    /// <summary>
    /// 
    /// </summary>
    public class ConsoleLoggerFactory : ILoggerFactory
    {
        public ILog GetLogger(Type type)
        {
            return new ConsoleLogger();
        }

        public ILog GetLogger(string name)
        {
            return new ConsoleLogger();
        }
    }
}