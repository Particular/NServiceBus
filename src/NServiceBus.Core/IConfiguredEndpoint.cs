namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using ObjectBuilder;

    /// <summary>
    /// Represents a configured endpoint ready to be started.
    /// </summary>
    public interface IConfiguredEndpoint
    {
        /// <summary>
        /// Starts the endpoint and returns a reference to it.
        /// </summary>
        /// <param name="builder">The adapter for the container's resolve API.</param>
        /// <returns>A reference to the endpoint.</returns>
        Task<IEndpointInstance> Start(IBuilder builder);

        /// <summary>
        /// Allows lazy access to the message session (singleton) so that it can be registered in depenency injection.
        /// Note: Only valid to use once the endpoint has started.
        /// </summary>
        Lazy<IMessageSession> MessageSession { get; }
    }
}
