namespace NServiceBus
{
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;

    /// <summary>
    /// Provides factory methods for creating endpoints instances with an externally managed container.
    /// </summary>
    public static class EndpointWithExternallyManagedContainer
    {
        /// <summary>
        /// Creates a new startable endpoint based on the provided configuration that uses an externally managed container.
        /// </summary>
        public static async Task<IStartableEndpointWithExternallyManagedContainer> Create(EndpointConfiguration configuration, IServiceCollection serviceCollection, CancellationToken cancellationToken = default)
        {
            Guard.AgainstNull(nameof(configuration), configuration);
            Guard.AgainstNull(nameof(serviceCollection), serviceCollection);

            return await HostCreator
                .CreateWithExternallyManagedContainer(configuration, serviceCollection, cancellationToken).ConfigureAwait(false);
        }
    }
}