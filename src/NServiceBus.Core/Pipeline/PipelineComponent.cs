namespace NServiceBus
{
    using ObjectBuilder;
    using Pipeline;

    class PipelineComponent
    {
        PipelineComponent(PipelineModifications modifications)
        {
            this.modifications = modifications;
        }

        public static PipelineComponent Initialize(PipelineSettings settings, HostingComponent hostingComponent)
        {
            var modifications = settings.modifications;

            foreach (var registeredBehavior in modifications.Replacements)
            {
                hostingComponent.Container.ConfigureComponent(registeredBehavior.BehaviorType, DependencyLifecycle.InstancePerCall);
            }

            foreach (var step in modifications.Additions)
            {
                step.ApplyContainerRegistration(hostingComponent.Container);
            }

            return new PipelineComponent(modifications);
        }

        public Pipeline<T> CreatePipeline<T>(IBuilder builder) where T : IBehaviorContext
        {
            return new Pipeline<T>(builder, modifications);
        }

        public PipelineCache BuildPipelineCache(IBuilder rootBuilder)
        {
            return new PipelineCache(rootBuilder, modifications);
        }

        readonly PipelineModifications modifications;
    }
}