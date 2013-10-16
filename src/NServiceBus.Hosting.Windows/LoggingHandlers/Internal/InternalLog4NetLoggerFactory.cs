namespace NServiceBus.Hosting.Windows.LoggingHandlers.Internal
{
    using System;
    using Logging;

    internal class InternalLog4NetLoggerFactory : ILoggerFactory
    {
        public ILog GetLogger(Type type)
        {
            return new InternalLog4NetLog(log4net.LogManager.GetLogger(type));
        }

        public ILog GetLogger(string name)
        {
            return new InternalLog4NetLog(log4net.LogManager.GetLogger(name));
        }
    }
}