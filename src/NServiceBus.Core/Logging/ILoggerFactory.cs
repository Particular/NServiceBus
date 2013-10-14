namespace NServiceBus.Logging
{
    using System;

    public interface ILoggerFactory
    {
        ILog GetLogger(Type type);
        ILog GetLogger(string name);
    }
}