using System.Threading;
using System.Threading.Tasks;
using NServiceBus.Transport;

namespace NServiceBus.Transports
{
    /// <summary>
    /// Abstraction of the capability to dispatch messages.
    /// </summary>
    public interface IMessageDispatcher
    {
        /// <summary>
        /// Dispatches the given operations to the transport.
        /// </summary>
        Task Dispatch(TransportOperations outgoingMessages, TransportTransaction transaction, CancellationToken cancellationToken = default);
    }
}