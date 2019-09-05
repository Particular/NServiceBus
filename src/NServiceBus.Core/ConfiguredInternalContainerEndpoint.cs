namespace NServiceBus
{
    using Settings;

    class ConfiguredInternalContainerEndpoint : ConfiguredEndpoint
    {
        public ConfiguredInternalContainerEndpoint(SettingsHolder settings,
            ContainerComponent containerComponent,
            PipelineComponent pipelineComponent) : base(settings, containerComponent, pipelineComponent)
        {
        }
    }
}