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
        /// Indicate that you don't want the host to automatically check if MSMQ is installed,
        /// install MSMQ if it isn't, check that the right components of MSMQ are active,
        /// change the active MSMQ components as needed, check that the MSMQ service is running,
        /// and run the MSMQ service if it isn't.
        /// This can somewhat decrease startup times.
        /// </summary>
        public interface MsmqInitialization {}

        /// <summary>
        /// Indicate that you don't want the host to automatically check if the Distributed Transaction Coordinator
        /// windows service has its security settings configured correctly, and if they aren't, set the correct settings,
        /// check that the service is running, and if it isn't, run the MSDTC service.
        /// This can somewhat decrease startup times.
        /// </summary>
        public interface DtcInitialization {}

        /// <summary>
        /// Indicate to the host that you don't want the bus to be started automatically.
        /// </summary>
        public interface TheBusStartedAutomatically {}

        /// <summary>
        /// Indicate to the host that you don't want the bus to subscribe automatically to messages
        /// owned by other endpoints that are handled by this endpoint.
        /// </summary>
        public interface ToSubscribeAutomatically {}

        /// <summary>
        /// Indicate to the host that you don't want support for sagas.
        /// This can shorten startup times ever so slightly.
        /// </summary>
        public interface Sagas {}
    }
}
