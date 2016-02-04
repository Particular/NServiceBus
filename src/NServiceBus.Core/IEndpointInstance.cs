namespace NServiceBus
{
    using System.Threading.Tasks;

    /// <summary>
    /// Represents an endpoind in the running phase.
    /// </summary>
    public interface IEndpointInstance : IMessageSession
    {
        /// <summary>
        /// Stops the endpoint.
        /// </summary>
        Task Stop();
    }
}