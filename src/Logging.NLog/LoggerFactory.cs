namespace NServiceBus.NLog
{
    using System;
    using Logging;
    using NlogLogManager = global::NLog.LogManager;

    class LoggerFactory : ILoggerFactory
    {

        public ILog GetLogger(Type type)
        {
            var logger = NlogLogManager.GetLogger(type.FullName);
            return new Logger(logger);
        }

        public ILog GetLogger(string name)
        {
            var logger = NlogLogManager.GetLogger(name);
            return new Logger(logger);
        }
    }
}