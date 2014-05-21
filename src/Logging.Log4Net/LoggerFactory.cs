namespace NServiceBus.Log4Net
{
    using System;
    using Logging;

    class LoggerFactory : ILoggerFactory
    {

        public ILog GetLogger(Type type)
        {
            var logger = log4net.LogManager.GetLogger(type.FullName);
            return new Logger(logger);
        }

        public ILog GetLogger(string name)
        {
            var logger = log4net.LogManager.GetLogger(name);
            return new Logger(logger);
        }
    }
}