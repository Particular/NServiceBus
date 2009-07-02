using Common.Logging;

namespace NServiceBus.Host
{
    /// <summary>
    /// Indicate to the host that you don't want Log4Net to be used for logging.
    /// Implement this interface on the class which implements <see cref="IConfigureThisEndpoint"/>.
    /// </summary>
    public interface IDontWantLog4Net
    {
        /// <summary>
        /// Return an instance of an adapter to your own logging framework.
        /// </summary>
        ILoggerFactoryAdapter UseThisInstead { get; }
    }
}
