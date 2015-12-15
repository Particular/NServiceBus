namespace NServiceBus
{
    using System.Threading.Tasks;

    /// <summary>
    /// Represents an endpoind in the running phase.
    /// </summary>
    public interface IEndpointInstance : IBusSessionFactory
    {
        /// <summary>
        /// Stops the endpoint.
        /// </summary>
        Task Stop();
    }
}