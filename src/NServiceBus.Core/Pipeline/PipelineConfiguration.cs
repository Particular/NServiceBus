namespace NServiceBus
{
    using ObjectBuilder;
    using Settings;

    class PipelineConfiguration
    {
        public void RegisterBehaviorsInContainer(SettingsHolder settings, IConfigureComponents container)
        {
            foreach (var registeredBehavior in Modifications.Replacements)
            {
                container.ConfigureComponent(registeredBehavior.BehaviorType, DependencyLifecycle.InstancePerCall);
            }

            foreach (var step in Modifications.Additions)
            {
                step.ApplyContainerRegistration(settings, container);
            }
        }

        public PipelineModifications Modifications = new PipelineModifications();
    }
}