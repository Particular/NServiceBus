namespace NServiceBus.Logging
{
    using System;

    /// <summary>
    /// Used by <see cref="LogManager"/> to facilitate redirecting logging to a different library.
    /// </summary>
    public interface ILoggerFactory
    {
        ILog GetLogger(Type type);
        ILog GetLogger(string name);
    }
}