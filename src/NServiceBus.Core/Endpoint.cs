namespace NServiceBus
{
    using System;
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
        /// <param name="serviceCollection">The service collection instance to use.</param>
        /// <param name="containerFactory">The service provider factory.</param>
#pragma warning disable CS3001 // Argument type is not CLS-compliant
        public static Task<IStartableEndpoint> Create(EndpointConfiguration configuration, IServiceCollection serviceCollection, Func<IServiceProvider> containerFactory)
#pragma warning restore CS3001 // Argument type is not CLS-compliant
        {
            Guard.AgainstNull(nameof(configuration), configuration);
            Guard.AgainstNull(nameof(serviceCollection), serviceCollection);
            Guard.AgainstNull(nameof(containerFactory), containerFactory);

            return HostCreator.CreateWithInternallyManagedContainer(configuration, serviceCollection, containerFactory);
        }

        /// <summary>
        /// Creates and starts a new endpoint based on the provided configuration.
        /// </summary>
        /// <param name="configuration">Configuration.</param>
        /// <param name="serviceCollection">The service collection instance to use.</param>
        /// <param name="containerFactory">The service provider factory.</param>
#pragma warning disable CS3001 // Argument type is not CLS-compliant
        public static async Task<IEndpointInstance> Start(EndpointConfiguration configuration, IServiceCollection serviceCollection, Func<IServiceProvider> containerFactory)
#pragma warning restore CS3001 // Argument type is not CLS-compliant
        {
            Guard.AgainstNull(nameof(configuration), configuration);
            Guard.AgainstNull(nameof(serviceCollection), serviceCollection);
            Guard.AgainstNull(nameof(containerFactory), containerFactory);

            var startableEndpoint = await Create(configuration, serviceCollection, containerFactory).ConfigureAwait(false);

            return await startableEndpoint.Start().ConfigureAwait(false);
        }
    }
}