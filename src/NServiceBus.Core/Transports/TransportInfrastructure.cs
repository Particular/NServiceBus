using System.Collections.ObjectModel;
using System.Linq;
using NServiceBus.Settings;
using NServiceBus.Transport;

namespace NServiceBus.Transport
{
    using System.Threading.Tasks;

    /// <summary>
    /// Transport infrastructure definitions.
    /// </summary>
    public abstract class TransportInfrastructure
    {
        /// <summary>
        /// The dispatcher to send messages.
        /// </summary>
        public virtual IMessageDispatcher Dispatcher { get; protected set; }

        /// <summary>
        /// A list of all receivers.
        /// </summary>
        public virtual ReadOnlyCollection<IMessageReceiver> Receivers { get; protected set; }

        /// <summary>
        /// This method is used when the transport is hosted as part of an NServiceBus endpoint to allow the transport verification of endpoint settings.
        /// </summary>
        public virtual Task ValidateNServiceBusSettings(ReadOnlySettings settings)
        {
            // this is only called when the transport is hosted as part of NServiceBus. No need to call this as "raw users".
            // pass a settings type that only allows "tryGet".
            return Task.CompletedTask;
        }

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
        public abstract Task DisposeAsync();
    }
}