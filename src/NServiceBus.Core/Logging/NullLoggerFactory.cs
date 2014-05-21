namespace NServiceBus.Logging.Loggers
{
    using System;

    class NullLoggerFactory : ILoggerFactory
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