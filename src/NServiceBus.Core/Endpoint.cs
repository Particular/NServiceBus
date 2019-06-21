namespace NServiceBus
{
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
        public static IConfiguredEndpoint Configure(ExternalContainerEndpointConfiguration configuration)
        {
            Guard.AgainstNull(nameof(configuration), configuration);

            return configuration.Configure();
        }

        /// <summary>
        /// Creates a new startable endpoint based on the provided configuration.
        /// </summary>
        /// <param name="configuration">Configuration.</param>
        public static IInstallableEndpoint Configure(EndpointConfiguration configuration)
        {
            Guard.AgainstNull(nameof(configuration), configuration);

            return configuration.Configure();
        }

        /// <summary>
        /// Creates a new startable endpoint based on the provided configuration.
        /// </summary>
        /// <param name="configuration">Configuration.</param>
        public static async Task<IStartableEndpoint> Create(EndpointConfiguration configuration)
        {
            Guard.AgainstNull(nameof(configuration), configuration);
            var installable = configuration.Configure();

            var startable = await installable.Install().ConfigureAwait(false);

            return startable;
        }

        /// <summary>
        /// Creates and starts a new endpoint based on the provided configuration.
        /// </summary>
        /// <param name="configuration">Configuration.</param>
        public static async Task<IEndpointInstance> Start(EndpointConfiguration configuration)
        {
            var initializable = await Create(configuration).ConfigureAwait(false);
            return await initializable.Start().ConfigureAwait(false);
        }
    }
}