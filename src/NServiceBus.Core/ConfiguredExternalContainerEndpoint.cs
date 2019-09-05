namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using ObjectBuilder;

    class ConfiguredExternalContainerEndpoint : IConfiguredEndpoint
    {
        public ConfiguredExternalContainerEndpoint(ConfiguredEndpoint configuredEndpoint, ContainerComponent containerComponent)
        {
            this.configuredEndpoint = configuredEndpoint;
            this.containerComponent = containerComponent;

            MessageSession = new Lazy<IMessageSession>(() =>
            {
                if (messageSession == null)
                {
                    throw new InvalidOperationException("The message session can only be used after the endpoint is started.");
                }
                return messageSession;
            });
        }

        public Lazy<IMessageSession> MessageSession { get; private set; }

        public async Task<IEndpointInstance> Start(IBuilder builder)
        {
            containerComponent.UseExternallyManagedBuilder(builder);

            var startableEndpoint = await configuredEndpoint.Initialize().ConfigureAwait(false);
            var endpointInstance = await startableEndpoint.Start().ConfigureAwait(false);
            messageSession = endpointInstance;
            return endpointInstance;
        }

        ConfiguredEndpoint configuredEndpoint;
        ContainerComponent containerComponent;
        IMessageSession messageSession;
    }
}