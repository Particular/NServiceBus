namespace NServiceBus
{
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Provides factory methods for creating and starting endpoint instances.
    /// </summary>
    public static class Endpoint
    {
        /// <summary>
        /// Creates a new startable endpoint based on the provided configuration.
        /// </summary>
        /// <param name="configuration">Configuration.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
        public static Task<IStartableEndpoint> Create(EndpointConfiguration configuration, CancellationToken cancellationToken = default)
        {
            Guard.AgainstNull(nameof(configuration), configuration);

            return HostCreator.CreateWithInternallyManagedContainer(configuration, cancellationToken);
        }

        /// <summary>
        /// Creates and starts a new endpoint based on the provided configuration.
        /// </summary>
        /// <param name="configuration">Configuration.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
        public static async Task<IEndpointInstance> Start(EndpointConfiguration configuration, CancellationToken cancellationToken = default)
        {
            Guard.AgainstNull(nameof(configuration), configuration);

            var startableEndpoint = await Create(configuration, cancellationToken).ConfigureAwait(false);

            return await startableEndpoint.Start(cancellationToken).ConfigureAwait(false);
        }
    }
}