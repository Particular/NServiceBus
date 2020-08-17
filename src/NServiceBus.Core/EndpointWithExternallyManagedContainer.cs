namespace NServiceBus
{
    using Microsoft.Extensions.DependencyInjection;

    /// <summary>
    /// Provides factory methods for creating endpoints instances with an externally managed container.
    /// </summary>
    public static class EndpointWithExternallyManagedContainer
    {
        /// <summary>
        /// Creates a new startable endpoint based on the provided configuration that uses an externally managed container.
        /// </summary>
        public static IStartableEndpointWithExternallyManagedContainer Create(EndpointConfiguration configuration, IServiceCollection serviceCollection)
        {
            Guard.AgainstNull(nameof(configuration), configuration);
            Guard.AgainstNull(nameof(serviceCollection), serviceCollection);

            return HostCreator
                .CreateWithExternallyManagedContainer(configuration, serviceCollection);
        }
    }
}