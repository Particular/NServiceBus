namespace NServiceBus.Transport
{
    using System.Collections.ObjectModel;
    using System.Linq;
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
        /// A list of all receivers.
        /// </summary>
        public ReadOnlyCollection<IMessageReceiver> Receivers { get; protected set; }

        /// <summary>
        /// A helper method to find a receiver inside the <see cref="Receivers"/> collection with a specific id.
        /// </summary>
        public IMessageReceiver GetReceiver(string receiverId)
        {
            return Receivers.SingleOrDefault(r => r.Id == receiverId);
        }

        /// <summary>
        /// Disposes all transport internal resources.
        /// </summary>
        public abstract Task Shutdown();
    }
}