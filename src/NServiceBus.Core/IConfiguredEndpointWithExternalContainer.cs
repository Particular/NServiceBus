namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using ObjectBuilder;

    /// <summary>
    /// Represents a configured endpoint ready to be started.
    /// </summary>
    public interface IConfiguredEndpointWithExternalContainer
    {
        /// <summary>
        /// Starts the endpoint and returns a reference to it.
        /// </summary>
        /// <param name="builder">The adapter for the container's resolve API.</param>
        /// <returns>A reference to the endpoint.</returns>
        Task<IEndpointInstance> Start(IBuilder builder);

        /// <summary>
        /// Access to the singleton `IMessageSession` to be registered in dependency injection container.
        /// Note: Lazily resolved since it's only valid for use once the endpoint has started.
        /// </summary>
        Lazy<IMessageSession> MessageSession { get; }
    }
}