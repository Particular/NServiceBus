namespace NServiceBus
{
    using System;
    using System.Runtime.ExceptionServices;
    using System.Threading.Tasks;
    using ObjectBuilder;

    /// <summary>
    /// Represents an endpoint in the start-up phase where the container is externally managed.
    /// </summary>
    public interface IStartableEndpointWithExternallyManagedContainer
    {
        /// <summary>
        /// Starts the endpoint and returns a reference to it.
        /// </summary>
        /// <param name="builder">The adapter for the container's resolve API.</param>
        /// <returns>A reference to the endpoint.</returns>
        Task<IEndpointInstance> Start(IBuilder builder);

        /// <summary>
        /// Access to the singleton IMessageSession to be registered in dependency injection container.
        /// Note: Lazily resolved since it's only valid for use once the endpoint has started.
        /// </summary>
        Lazy<IMessageSession> MessageSession { get; }
    }
}