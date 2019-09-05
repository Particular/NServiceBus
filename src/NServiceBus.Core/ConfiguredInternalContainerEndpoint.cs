namespace NServiceBus
{
    using NServiceBus.Features;
    using NServiceBus.Settings;
    using NServiceBus.Transport;

    class ConfiguredInternalContainerEndpoint : ConfiguredEndpoint
    {
        public ConfiguredInternalContainerEndpoint(ReceiveComponent receiveComponent, QueueBindings queueBindings, FeatureActivator featureActivator, TransportInfrastructure transportInfrastructure, CriticalError criticalError, SettingsHolder settings, PipelineComponent pipelineComponent, ContainerComponent containerComponent) : base(receiveComponent, queueBindings, featureActivator, transportInfrastructure, criticalError, settings, pipelineComponent, containerComponent)
        {
        }
    }
}