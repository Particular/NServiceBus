namespace NServiceBus
{
    using System.Threading.Tasks;
    using ObjectBuilder;

    /// <summary>
    /// Provides factory methods for creating and starting endpoint instances.
    /// </summary>
    public static class Endpoint
    {
        internal static PreparedEndpoint Prepare(EndpointConfiguration configuration, IConfigureComponents configureComponents)
        {
            Guard.AgainstNull(nameof(configuration), configuration);
            Guard.AgainstNull(nameof(configureComponents), configureComponents);

            //TODO: check if user is using anything container-related and throw

            var initializable = configuration.Build(configureComponents);
            var prepared = initializable.Prepare();

            configureComponents.ConfigureComponent(_ => prepared.Builder, DependencyLifecycle.SingleInstance);

            return prepared;
        }

        internal static async Task<IEndpointInstance> Start(PreparedEndpoint preparedEndpoint, IBuilder builder)
        {
            var initialized = await preparedEndpoint.Initialize(builder).ConfigureAwait(false);
            return await initialized.Start().ConfigureAwait(false);
        }

        /// <summary>
        /// Creates a new startable endpoint based on the provided configuration.
        /// </summary>
        /// <param name="configuration">Configuration.</param>
        public static async Task<IStartableEndpoint> Create(EndpointConfiguration configuration)
        {
            Guard.AgainstNull(nameof(configuration), configuration);

            var container = configuration.CustomContainer;

            if (container == null)
            {
                configuration.Settings.AddStartupDiagnosticsSection("Container", new
                {
                    Type = "internal"
                });
                container = new LightInjectObjectBuilder();
            }

            var containerType = configuration.CustomContainer.GetType();

            configuration.Settings.AddStartupDiagnosticsSection("Container", new
            {
                Type = containerType.FullName,
                Version = FileVersionRetriever.GetFileVersion(containerType)
            });

            var builder = new CommonObjectBuilder(container);

            var initializableEndpoint = configuration.Build(builder);
            var prepared = initializableEndpoint.Prepare();

            builder.ConfigureComponent<IBuilder>(_ => builder, DependencyLifecycle.SingleInstance);

            return await prepared.Initialize(builder).ConfigureAwait(false);
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