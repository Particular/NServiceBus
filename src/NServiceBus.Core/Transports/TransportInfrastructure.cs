using System.Collections.ObjectModel;
using System.Linq;
using NServiceBus.Settings;
using NServiceBus.Transports;

namespace NServiceBus.Transport
{
    using System.Threading.Tasks;

    /// <summary>
    /// Transport infrastructure definitions.
    /// </summary>
    public abstract class TransportInfrastructure
    {
        /// <summary>
        /// 
        /// </summary>
        public virtual IMessageDispatcher Dispatcher { get; protected set; }

        /// <summary>
        /// </summary>
        public virtual ReadOnlyCollection<IMessageReceiver> Receivers { get; protected set; }

        /// <summary>
        /// 
        /// </summary>
        public virtual Task ValidateNServiceBusSettings(ReadOnlySettings settings)
        {
            // this is only called when the transport is hosted as part of NServiceBus. No need to call this as "raw users".
            // pass a settings type that only allows "tryGet".
            return Task.CompletedTask;
        }

        /// <summary>
        /// 
        /// </summary>
        public IMessageReceiver GetReceiver(string receiverId)
        {
            return Receivers.SingleOrDefault(r => r.Id == receiverId);
        }


        /// <summary>
        /// </summary>
        public abstract Task DisposeAsync();
    }
}