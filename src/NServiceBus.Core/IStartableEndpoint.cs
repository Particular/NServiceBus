namespace NServiceBus
{
	using System.Threading.Tasks;

	/// <summary>
    /// Represents an endpoint int the start-up phase.
    /// </summary>
    public interface IStartableEndpoint
    {
        /// <summary>
        /// Starts the bus and returns a reference to it.
        /// </summary>
        /// <returns>A reference to the bus.</returns>
        Task<IEndpointInstance> Start();
    }
}