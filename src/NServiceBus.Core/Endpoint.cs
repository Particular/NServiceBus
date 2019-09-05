namespace NServiceBus
{
    using System.Threading.Tasks;
    using ObjectBuilder;

    /// <summary>
    /// Factory methods for configuring, creating and starting endpoint instances.
    /// </summary>
    public static class Endpoint
    {
        /// <summary>
        /// Configures an endpoint to run with an externally managed container.
        /// </summary>
        /// <param name="configuration">The endpoint configuration.</param>
        /// <param name="configureComponents">The registration API adapter for the external container.</param>
        /// <returns>The configured endpoint.</returns>
        public static IConfiguredEndpointWithExternallyManagedContainer ConfigureWithExternallyManagedContainer(EndpointConfiguration configuration, IConfigureComponents configureComponents)
        {
            Guard.AgainstNull(nameof(configuration), configuration);
            Guard.AgainstNull(nameof(configureComponents), configureComponents);

            return ConfiguredEndpoint
                .ConfigureWithExternallyManagedContainer(configuration, configureComponents);
        }

        /// <summary>
        /// Creates a new startable endpoint based on the provided configuration.
        /// </summary>
        /// <param name="configuration">Configuration.</param>
        public static Task<IStartableEndpoint> Create(EndpointConfiguration configuration)
        {
            Guard.AgainstNull(nameof(configuration), configuration);

            return ConfiguredEndpoint
                .ConfigureWithInternallyManagedContainer(configuration)
                .CreateStartableEndpoint();
        }

        /// <summary>
        /// Creates and starts a new endpoint based on the provided configuration.
        /// </summary>
        /// <param name="configuration">Configuration.</param>
        public static async Task<IEndpointInstance> Start(EndpointConfiguration configuration)
        {
            Guard.AgainstNull(nameof(configuration), configuration);
            var startableEndpoint = await Create(configuration).ConfigureAwait(false);

            return await startableEndpoint.Start().ConfigureAwait(false);
        }
    }
}