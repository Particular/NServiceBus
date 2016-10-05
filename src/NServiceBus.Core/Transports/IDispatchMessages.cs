namespace NServiceBus.Transport
{
    using System.Threading.Tasks;
    using Extensibility;

    /// <summary>
    /// Abstraction of the capability to dispatch messages.
    /// </summary>
    public interface IDispatchMessages
    {
        /// <summary>
        /// Dispatches the given operations to the transport.
        /// </summary>
        Task Dispatch(TransportOperations outgoingMessages, TransportTransaction transaction, ContextBag context);
    }
}