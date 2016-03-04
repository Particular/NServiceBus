namespace NServiceBus.Logging
{
    /// <summary>
    /// Base class for logging definitions.
    /// </summary>
    public abstract class LoggingFactoryDefinition
    {
        /// <summary>
        /// Constructs an instance of <see cref="ILoggerFactory" /> for use by <see cref="LogManager.Use{T}" />.
        /// </summary>
        protected internal abstract ILoggerFactory GetLoggingFactory();
    }
}