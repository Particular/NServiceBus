namespace NServiceBus
{
    using ObjectBuilder;

    class PipelineConfiguration
    {
        public void RegisterBehaviorsInContainer(IConfigureComponents container)
        {
            foreach (var registeredBehavior in Modifications.Replacements)
            {
                container.ConfigureComponent(registeredBehavior.BehaviorType, DependencyLifecycle.InstancePerCall);
            }

            foreach (var step in Modifications.Additions)
            {
                step.ApplyContainerRegistration(container);
            }
        }

        public PipelineModifications Modifications = new PipelineModifications();
    }
}