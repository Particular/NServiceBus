namespace NServiceBus
{
    using System.Threading.Tasks;

    /// <summary>
    /// Provides factory methods for creating endpoint instances.
    /// </summary>
    public static class Endpoint
    {
        /// <summary>
        /// Creates a new initializable endpoint based on the provided configuration.
        /// </summary>
        /// <param name="configuration">Configuration.</param>
        public static IInitializableEndpoint Prepare(BusConfiguration configuration)
        {
            Guard.AgainstNull(nameof(configuration), configuration);
            var endpoint = configuration.Build();
            return endpoint;
        }

        /// <summary>
        /// Creates a new startable endpoint based on the provided configuration.
        /// </summary>
        /// <param name="configuration">Configuration.</param>
        public static Task<IStartableEndpoint> Create(BusConfiguration configuration)
        {
            Guard.AgainstNull(nameof(configuration), configuration);
            var initializable = Prepare(configuration);
            return initializable.Initialize();
        }

        /// <summary>
        /// Creates and starts a new endpoint based on the provided configuration.
        /// </summary>
        /// <param name="configuration">Configuration.</param>
        public static async Task<IEndpointInstance> Start(BusConfiguration configuration)
        {
            var initializable = await Create(configuration).ConfigureAwait(false);
            return await initializable.Start().ConfigureAwait(false);
        }
    }
}