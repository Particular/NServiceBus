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
        /// Prepares an endpoint to run with and externally managed container.
        /// </summary>
        /// <param name="configuration">The endpoint configuration.</param>
        /// <param name="configureComponents">The registration api adapter for the external container.</param>
        /// <returns>The prepared endpoint.</returns>
        public static IConfiguredEndpoint Configure(EndpointConfiguration configuration, IConfigureComponents configureComponents)
        {
            Guard.AgainstNull(nameof(configuration), configuration);
            Guard.AgainstNull(nameof(configureComponents), configureComponents);

            var provideBuilder = configuration.UseExternallyManagedContainer(configureComponents);

            var configurableEndpoint = configuration.Build();
            var configuredEndpoint = configurableEndpoint.Configure();

            return new ConfiguredExternalContainerEndpoint(configuredEndpoint, configureComponents, provideBuilder);
        }

        /// <summary>
        /// Creates a new startable endpoint based on the provided configuration.
        /// </summary>
        /// <param name="configuration">Configuration.</param>
        public static Task<IStartableEndpoint> Create(EndpointConfiguration configuration)
        {
            Guard.AgainstNull(nameof(configuration), configuration);

            var initializableEndpoint = configuration.Build();

            var preparedEndpoint = initializableEndpoint.Configure();

            return preparedEndpoint.Initialize();
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