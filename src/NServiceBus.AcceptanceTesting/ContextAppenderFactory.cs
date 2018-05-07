namespace NServiceBus.AcceptanceTesting
{
    using System;
    using Logging;

    class ContextAppenderFactory : ILoggerFactory
    {
        public ILog GetLogger(Type type)
        {
            return GetLogger(type.FullName);
        }

        public ILog GetLogger(string name)
        {
            return new ContextAppender(name);
        }
    }
}