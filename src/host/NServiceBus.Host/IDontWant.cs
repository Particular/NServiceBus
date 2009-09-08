using Common.Logging;

namespace NServiceBus.Host
{
    /// <summary>
    /// Container class for interface specifications.
    /// Implement the contained interfaces on the class which implements <see cref="IConfigureThisEndpoint"/>.
    /// </summary>
    public class IDontWant
    {
        /// <summary>
        /// Indicate to the host that you don't want Log4Net to be used for logging.
        /// </summary>
        public interface Log4Net
        {
            /// <summary>
            /// Return an instance of an adapter to your own logging framework.
            /// </summary>
            ILoggerFactoryAdapter UseThisInstead { get; }
        }

        /// <summary>
        /// Indicate to the host that you don't want the bus to be started automatically.
        /// </summary>
        public interface TheBusStartedAutomatically {}

        /// <summary>
        /// Indicate to the host that you don't want the bus to subscribe automatically to messages
        /// owned by other endpoints that are handled by this endpoint.
        /// </summary>
        public interface ToSubscribeAutomatically {}
    }
}
