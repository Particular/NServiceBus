namespace NServiceBus.Pipeline
{
    using System.Collections.Generic;
    using NServiceBus.ObjectBuilder;
    using NServiceBus.Settings;

    class PipelinesCollection
    {
        public readonly PipelineModifications MainPipeline = new PipelineModifications();
        public readonly List<SatellitePipelineModifications> SatellitePipelines = new List<SatellitePipelineModifications>();
        public RegisterStep ReceiveBehavior { get; set; }

        public void RegisterBehaviorsInContainer(SettingsHolder settings, IConfigureComponents container)
        {
            if (ReceiveBehavior != null)
            {
                ReceiveBehavior.ApplyContainerRegistration(settings, container);
            }
            RegisterBehaviorsInContainer(MainPipeline, settings, container);
            foreach (var satellitePipeline in SatellitePipelines)
            {
                RegisterBehaviorsInContainer(satellitePipeline, settings, container);
            }
        }

        void RegisterBehaviorsInContainer(PipelineModifications pipeline, SettingsHolder settings, IConfigureComponents container)
        {
            foreach (var registeredBehavior in pipeline.Replacements)
            {
                container.ConfigureComponent(registeredBehavior.BehaviorType, DependencyLifecycle.InstancePerCall);
            }

            foreach (var step in pipeline.Additions)
            {
                step.ApplyContainerRegistration(settings, container);
            }
        }
    }
}