namespace NServiceBus.Transport
{
    using System.Threading;
    using System.Threading.Tasks;

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