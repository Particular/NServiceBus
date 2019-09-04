namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using ObjectBuilder;

    /// <summary>
    /// Represents an endpoint in the start-up phase.
    /// </summary>
    public interface IConfiguredEndpoint
    {
        /// <summary>
        /// Starts the endpoint and returns a reference to it.
        /// </summary>
        /// <param name="builder">The adapter for the containers resolve API.</param>
        /// <returns>A reference to the endpoint.</returns>
        Task<IEndpointInstance> Start(IBuilder builder);

        /// <summary>
        /// Allows lazy access to the message session (singleton) so that it can be registered in depenency injection.
        /// Note: Only valid to use once the endpoint have started.
        /// </summary>
        Lazy<IMessageSession> MessageSession { get; }
    }
}