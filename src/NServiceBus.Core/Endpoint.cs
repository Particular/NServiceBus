namespace NServiceBus
{
    using System.Threading.Tasks;

    /// <summary>
    /// Provides factory methods for creating endpoint instances.
    /// </summary>
    public static class Endpoint
    {
        /// <summary>
        /// Creates a new endpoint based on the provided configuration.
        /// </summary>
        /// <param name="configuration">Configuration.</param>
        public static IInitializableEndpoint Create(BusConfiguration configuration)
        {
            Guard.AgainstNull(nameof(configuration), configuration);
            var endpoint = configuration.Build();
            return endpoint;
        }

        /// <summary>
        /// Creates and starts a new endpoint based on the provided configuration.
        /// </summary>
        /// <param name="configuration">Configuration.</param>
        public static async Task<IEndpoint> Start(BusConfiguration configuration)
        {
            var initializable = Create(configuration);
            var startable = await initializable.Initialize().ConfigureAwait(false);
            return await startable.Start().ConfigureAwait(false);
        }
    }

    
}