using System.Threading.Tasks;

namespace NServiceBus.Raw
{
    /// <summary>
    /// Provides factory methods for creating and starting endpoint instances.
    /// </summary>
    public static class RawEndpoint
    {
        /// <summary>
        /// Creates a new startable endpoint based on the provided configuration.
        /// </summary>
        /// <param name="configuration">Configuration.</param>
        public static Task<IStartableRawEndpoint> Create(RawEndpointConfiguration configuration)
        {
            Guard.AgainstNull(nameof(configuration), configuration);
            var initializable = configuration.Build();
            return initializable.Initialize();
        }

        /// <summary>
        /// Creates and starts a new endpoint based on the provided configuration.
        /// </summary>
        /// <param name="configuration">Configuration.</param>
        public static async Task<IReceivingRawEndpoint> Start(RawEndpointConfiguration configuration)
        {
            var initializable = await Create(configuration).ConfigureAwait(false);
            return await initializable.Start().ConfigureAwait(false);
        }
    }
}