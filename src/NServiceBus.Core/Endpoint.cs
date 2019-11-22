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
        public static Task<IStartableEndpoint> Create(EndpointConfiguration configuration)
        {
            Guard.AgainstNull(nameof(configuration), configuration);

            return EndpointCreator.CreateWithInternallyManagedContainer(configuration);
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