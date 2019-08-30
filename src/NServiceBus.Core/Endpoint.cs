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
        public static PreparedEndpoint Prepare(EndpointConfiguration configuration, IConfigureComponents configureComponents)
        {
            Guard.AgainstNull(nameof(configuration), configuration);
            Guard.AgainstNull(nameof(configureComponents), configureComponents);

            configuration.UseExternallyManagedContainer(configureComponents);

            var initializable = configuration.Build();

            return initializable.Prepare();
        }

        /// <summary>
        /// Starts a prepared endpoint.
        /// </summary>
        /// <param name="preparedEndpoint">The prepared endpoint.</param>
        /// <param name="builder">The adapter for the containers resolve API.</param>
        /// <returns>A started endpoint instance.</returns>
        public static async Task<IEndpointInstance> Start(PreparedEndpoint preparedEndpoint, IBuilder builder)
        {
            preparedEndpoint.UseExternallyManagedBuilder(builder);

            var initialized = await preparedEndpoint.Initialize().ConfigureAwait(false);

            return await initialized.Start().ConfigureAwait(false);
        }

        /// <summary>
        /// Creates a new startable endpoint based on the provided configuration.
        /// </summary>
        /// <param name="configuration">Configuration.</param>
        public static async Task<IStartableEndpoint> Create(EndpointConfiguration configuration)
        {
            Guard.AgainstNull(nameof(configuration), configuration);

            var initializableEndpoint = configuration.Build();

            var preparedEndpoint = initializableEndpoint.Prepare();

            return await preparedEndpoint.Initialize().ConfigureAwait(false);
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