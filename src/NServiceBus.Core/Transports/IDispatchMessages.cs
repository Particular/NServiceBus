namespace NServiceBus.Transports
{
    using System.Threading.Tasks;
    using System.Collections.Generic;

    /// <summary>
    /// Abstraction of the capability to dispatch messages.
    /// </summary>
    public interface IDispatchMessages
    {
        /// <summary>
        /// Dispatches the given operations to the transport.
        /// </summary>
        Task Dispatch(IEnumerable<TransportOperation> outgoingMessages);
    }
}