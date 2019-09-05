namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using Features;
    using ObjectBuilder;
    using Settings;
    using Transport;

    class ConfiguredExternalContainerEndpoint : ConfiguredEndpoint, IConfiguredEndpointWithExternalContainer
    {
        public ConfiguredExternalContainerEndpoint(ReceiveComponent receiveComponent, QueueBindings queueBindings, FeatureActivator featureActivator, TransportInfrastructure transportInfrastructure, CriticalError criticalError, SettingsHolder settings, PipelineComponent pipelineComponent, ContainerComponent containerComponent)
            : base(receiveComponent,  queueBindings,  featureActivator,  transportInfrastructure,  criticalError,  settings,  pipelineComponent,  containerComponent)
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

            var startableEndpoint = await Initialize().ConfigureAwait(false);
            var endpointInstance = await startableEndpoint.Start().ConfigureAwait(false);

            messageSession = endpointInstance;

            return endpointInstance;
        }

        ContainerComponent containerComponent;
        IMessageSession messageSession;
    }
}