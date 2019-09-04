namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using ObjectBuilder;

    class ConfiguredExternalContainerEndpoint : IConfiguredEndpoint
    {
        ConfiguredEndpoint configuredEndpoint;
        Action<IBuilder> provideBuilder;
        IMessageSession messageSession;

        public ConfiguredExternalContainerEndpoint(ConfiguredEndpoint configuredEndpoint, IConfigureComponents configureComponents, Action<IBuilder> provideBuilder)
        {
            this.configuredEndpoint = configuredEndpoint;
            this.provideBuilder = provideBuilder;
            configureComponents.ConfigureComponent(_ =>
            {
                if (messageSession == null)
                {
                    throw new InvalidOperationException("The message session can only be used after the endpoint is started.");
                }
                return messageSession;
            }, DependencyLifecycle.SingleInstance);
        }

        public async Task<IEndpointInstance> Start(IBuilder builder)
        {
            provideBuilder(builder);

            var startableEndpoint = await configuredEndpoint.Initialize().ConfigureAwait(false);
            var startedEndpoint = await startableEndpoint.Start().ConfigureAwait(false);
            messageSession = startedEndpoint;
            return startedEndpoint;
        }
    }
}