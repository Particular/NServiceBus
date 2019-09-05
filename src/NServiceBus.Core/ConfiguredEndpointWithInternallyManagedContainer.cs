namespace NServiceBus
{
    using Settings;

    class ConfiguredEndpointWithInternallyManagedContainer : ConfiguredEndpoint
    {
        public ConfiguredEndpointWithInternallyManagedContainer(SettingsHolder settings,
            ContainerComponent containerComponent,
            PipelineComponent pipelineComponent) : base(settings, containerComponent, pipelineComponent)
        {
        }
    }
}