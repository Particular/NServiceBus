namespace NServiceBus
{
    using System.Threading.Tasks;
    using Extensibility;
    using Transport;

    /// <summary>
    /// Represents an endpoint in the running phase.
    /// </summary>
    public interface IRawEndpointInstance
    {
        /// <summary>
        /// Sends a a collection of messages.
        /// </summary>
        /// <param name="outgoingMessages">Messages.</param>
        /// <param name="transaction">Transport transaction.</param>
        /// <param name="context">Context information.</param>
        Task SendRaw(TransportOperations outgoingMessages, TransportTransaction transaction, ContextBag context);
        /// <summary>
        /// Stops the endpoint.
        /// </summary>
        Task Stop();
    }
}