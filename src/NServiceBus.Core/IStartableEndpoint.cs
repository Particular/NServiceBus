namespace NServiceBus
{
    using System.Threading.Tasks;
    using ObjectBuilder;

    /// <summary>
    /// Represents an endpoint in the start-up phase.
    /// </summary>
    public interface IStartableEndpoint
    {
        /// <summary>
        /// Starts the endpoint and returns a reference to it.
        /// </summary>
        /// <returns>A reference to the endpoint.</returns>
        Task<IEndpointInstance> Start();
    }

    /// <summary>
    /// Represents an endpoint in a stage prior to setting up DI infrastructure.
    /// </summary>
    public interface IConfiguredEndpoint
    {
        /// <summary>
        /// Provides the endpoint with configured DI infrastructure.
        /// </summary>
        IInstallableEndpoint UseBuilder(IBuilder builder);
    }

    /// <summary>
    /// Represents an endpoint in the configured phase, prior to installing or starting.
    /// </summary>
    public interface IInstallableEndpoint : IStartableEndpoint
    {
        /// <summary>
        /// Runs the installers if they were enabled in the endpoint configuration.
        /// </summary>
        /// <returns>A reference to the endpoint.</returns>
        Task<IStartableEndpoint> Install();
    }
}