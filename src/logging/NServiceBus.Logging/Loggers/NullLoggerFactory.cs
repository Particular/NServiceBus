using System;

namespace NServiceBus.Logging.Loggers
{
    /// <summary>
    /// 
    /// </summary>
    public class NullLoggerFactory : ILoggerFactory
    {
        public ILog GetLogger(Type type)
        {
            return new NullLogger();
        }

        public ILog GetLogger(string name)
        {
            return new NullLogger();
        }
    }
}