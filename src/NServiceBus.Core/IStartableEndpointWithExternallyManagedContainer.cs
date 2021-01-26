namespace NServiceBus
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Represents an endpoint in the start-up phase where the container is externally managed.
    /// </summary>
    public interface IStartableEndpointWithExternallyManagedContainer
    {
        /// <summary>
        /// Starts the endpoint and returns a reference to it.
        /// </summary>
        /// <param name="builder">The <see cref="IServiceProvider"/> instance used to resolve dependencies.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
        /// <returns>A reference to the endpoint.</returns>
        Task<IEndpointInstance> Start(IServiceProvider builder, CancellationToken cancellationToken = default);

        /// <summary>
        /// Access to the singleton IMessageSession to be registered in dependency injection container.
        /// Note: Lazily resolved since it's only valid for use once the endpoint has started.
        /// </summary>
        Lazy<IMessageSession> MessageSession { get; }
    }
}