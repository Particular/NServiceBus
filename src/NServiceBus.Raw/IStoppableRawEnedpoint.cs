namespace NServiceBus.Raw
{
    using System.Threading.Tasks;

    /// <summary>
    /// Represents an endpoint in the shutdown phase.
    /// </summary>
    public interface IStoppableRawEndpoint
    {
        /// <summary>
        /// Stops the endpoint.
        /// </summary>
        Task Stop();
    }
}