namespace NServiceBus
{
    using System.Threading.Tasks;

    /// <summary>
    /// Represents an endpoind in the running phase.
    /// </summary>
    public interface IStoppableEndpoint
    {
        /// <summary>
        /// Stops the endpoint.
        /// </summary>
        Task StopAsync();

        /// <summary>
        /// Creates a context for sending out messages.
        /// </summary>
        IBus CreateOutgoingContext();
    }
}