namespace NServiceBus
{
    using System.Threading.Tasks;
    using ObjectBuilder;

    /// <summary>
    /// Provides factory methods for creating and starting endpoint instances.
    /// </summary>
    public static class Endpoint
    {
        /// <summary>
        /// Configures an endpoint to run with a externally managed container.
        /// </summary>
        /// <param name="configuration">The endpoint configuration.</param>
        /// <param name="configureComponents">The registration API adapter for the external container.</param>
        /// <returns>The configured endpoint.</returns>
        public static IConfiguredEndpoint Configure(EndpointConfiguration configuration, IConfigureComponents configureComponents)
        {
            Guard.AgainstNull(nameof(configuration), configuration);
            Guard.AgainstNull(nameof(configureComponents), configureComponents);

            var configurableEndpoint = configuration.Build();

            return configurableEndpoint.ConfigureWithExternalContainer(configureComponents);
        }

        /// <summary>
        /// Creates a new startable endpoint based on the provided configuration.
        /// </summary>
        /// <param name="configuration">Configuration.</param>
        public static Task<IStartableEndpoint> Create(EndpointConfiguration configuration)
        {
            Guard.AgainstNull(nameof(configuration), configuration);

            var configurableEndpoint = configuration.Build();

            var configuredEndpoint = configurableEndpoint.ConfigureWithInternalContainer();

            return configuredEndpoint.Initialize();
        }

        /// <summary>
        /// Creates and starts a new endpoint based on the provided configuration.
        /// </summary>
        /// <param name="configuration">Configuration.</param>
        public static async Task<IEndpointInstance> Start(EndpointConfiguration configuration)
        {
            var initializableEndpoint = await Create(configuration).ConfigureAwait(false);

            return await initializableEndpoint.Start().ConfigureAwait(false);
        }
    }
}