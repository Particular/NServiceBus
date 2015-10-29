namespace NServiceBus
{
    using System.Threading.Tasks;

    /// <summary>
    /// Represents an endpoint in the initialize phase.
    /// </summary>
    public interface IInitializableEndpoint
    {
        /// <summary>
        /// Initializes this endpoint, performing all the necessary start-up checks preparing the endpoint to start.
        /// </summary>
        /// <returns>A reference to the endpoint that allows it to start.</returns>
        Task<IStartableEndpoint> Initialize();
    }
}