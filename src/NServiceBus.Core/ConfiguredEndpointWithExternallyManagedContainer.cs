namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using ObjectBuilder;
    using Settings;

    class ConfiguredEndpointWithExternallyManagedContainer : ConfiguredEndpoint, IConfiguredEndpointWithExternallyManagedContainer
    {
        public ConfiguredEndpointWithExternallyManagedContainer(SettingsHolder settings,
            ContainerComponent containerComponent,
            PipelineComponent pipelineComponent) : base(settings, containerComponent, pipelineComponent)
        {
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

            var startableEndpoint = await CreateStartableEndpoint().ConfigureAwait(false);
            var endpointInstance = await startableEndpoint.Start().ConfigureAwait(false);

            messageSession = endpointInstance;

            return endpointInstance;
        }

        ContainerComponent containerComponent;
        IMessageSession messageSession;
    }
}