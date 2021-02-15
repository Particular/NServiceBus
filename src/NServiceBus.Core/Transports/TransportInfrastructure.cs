namespace NServiceBus.Transport
{
    using System.Collections.ObjectModel;
    using System.Threading.Tasks;

    /// <summary>
    /// Transport infrastructure definitions.
    /// </summary>
    public abstract class TransportInfrastructure
    {
        /// <summary>
        /// The dispatcher to send messages.
        /// </summary>
        public IMessageDispatcher Dispatcher { get; protected set; }

        /// <summary>
        /// A collection of all receivers.
        /// </summary>
        public ReadOnlyDictionary<string, IMessageReceiver> Receivers { get; protected set; }

        /// <summary>
        /// Disposes all transport internal resources.
        /// </summary>
        public abstract Task Shutdown();
    }
}