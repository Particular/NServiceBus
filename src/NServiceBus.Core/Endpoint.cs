namespace NServiceBus
{
    using System.Threading.Tasks;
    using ObjectBuilder;

    /// <summary>
    /// Provides factory methods for creating and starting endpoint instances.
    /// </summary>
    public static class Endpoint
    {
        class BuilderHolder
        {
            IBuilder builder;

            public IBuilder Builder => builder;

            public void Initialize(IBuilder builder)
            {
                this.builder = builder;
            }
        }

        internal static void Prepare(EndpointConfiguration configuration, IConfigureComponents configureComponents)
        {
            Guard.AgainstNull(nameof(configuration), configuration);

            //TODO: check if user is using anything container-related and throw

            var initializable = configuration.Build(configureComponents);
            var prepared = initializable.Prepare();
            var holder = new BuilderHolder();
            configureComponents.ConfigureComponent(_ => prepared, DependencyLifecycle.SingleInstance);
            configureComponents.ConfigureComponent(_ => holder, DependencyLifecycle.SingleInstance);
            configureComponents.ConfigureComponent(_ => holder.Builder, DependencyLifecycle.SingleInstance);
        }

        internal static async Task<IEndpointInstance> Start(IBuilder builder)
        {
            var preparedEndpoint = builder.Build<PreparedEndpoint>();
            var builderHolder = builder.Build<BuilderHolder>();
            builderHolder.Initialize(builder);
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

            var container = configuration.ConfigureContainer();
            var builder = new CommonObjectBuilder(container);

            var initializable = configuration.Build(builder);
            var prepared = initializable.Prepare();
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