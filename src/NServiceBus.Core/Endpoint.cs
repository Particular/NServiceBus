namespace NServiceBus
{
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;

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
        public static async Task<IStartableEndpoint> Create(EndpointConfiguration configuration, CancellationToken cancellationToken = default)
        {
            Guard.AgainstNull(nameof(configuration), configuration);
            var serviceCollection = new ServiceCollection();
            var host = HostCreator.BuildEndpointCreator(configuration, serviceCollection);

            var serviceProvider = serviceCollection.BuildServiceProvider();

            var endpoint = host.CreateStartableEndpoint(serviceProvider, true);

            if (endpoint.HostingConfiguration.ShouldRunInstallers)
            {
                await endpoint.RunInstallers(cancellationToken).ConfigureAwait(false);
            }

            return new InternallyManagedContainerHost(endpoint);
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

        /// <summary>
        /// Executes all the installers and transport configuration without starting the endpoint.
        /// <see cref="Install"/> always runs installers, even if <see cref="InstallConfigExtensions.EnableInstallers"/> has not been configured.
        /// </summary>
        /// <param name="configuration">The endpoint configuration.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
        public static async Task Install(EndpointConfiguration configuration, CancellationToken cancellationToken = default)
        {
            Guard.AgainstNull(nameof(configuration), configuration);

            var serviceCollection = new ServiceCollection();
            var host = HostCreator.BuildEndpointCreator(configuration, serviceCollection);

            var serviceProvider = serviceCollection.BuildServiceProvider();
            await using (serviceProvider.ConfigureAwait(false))
            {
                var endpoint = host.CreateStartableEndpoint(serviceProvider, true);
                await endpoint.RunInstallers(cancellationToken).ConfigureAwait(false);
                await endpoint.Setup(cancellationToken).ConfigureAwait(false);
            }
        }
    }
}