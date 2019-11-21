namespace NServiceBus
{
    using System;
    using ObjectBuilder;
    using Pipeline;

    class PipelineComponent
    {
        PipelineComponent(PipelineSettings pipelineSettings)
        {
            this.pipelineSettings = pipelineSettings;
        }

        public static PipelineComponent Initialize(PipelineSettings settings, HostingComponent.Configuration hostingConfiguration)
        {
            var modifications = settings.modifications;

            foreach (var registeredBehavior in modifications.Replacements)
            {
                hostingConfiguration.Container.ConfigureComponent(registeredBehavior.BehaviorType, DependencyLifecycle.InstancePerCall);
            }

            foreach (var step in modifications.Additions)
            {
                step.ApplyContainerRegistration(hostingConfiguration.Container);
            }

            return new PipelineComponent(settings);
        }

        public Pipeline<T> CreatePipeline<T>(IBuilder builder) where T : IBehaviorContext
        {
            return new Pipeline<T>(builder, pipelineSettings.modifications);
        }

        public PipelineCache BuildPipelineCache(IBuilder rootBuilder)
        {
            return new PipelineCache(rootBuilder, pipelineSettings.modifications);
        }

        public void RegisterBehavior<T>(string stepId, Func<IBuilder, T> factoryMethod, string description)
            where T : IBehavior
        {
            pipelineSettings.Register(stepId, factoryMethod, description);
        }

        public void RegisterBehavior<T>(string stepId, T behavior, string description)
            where T : IBehavior
        {
            pipelineSettings.Register(stepId, behavior, description);
        }

        readonly PipelineSettings pipelineSettings;
    }
}