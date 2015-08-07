namespace NServiceBus.Pipeline
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using NServiceBus.ObjectBuilder;
    using NServiceBus.Settings;

    class PipelineConfiguration
    {
        readonly List<FeatureBehaviorsRegistration> featureBehaviorsRegistrations = new List<FeatureBehaviorsRegistration>();
        readonly PipelineModificationsBuilder userDefinedBehaviors;

        public PipelineConfiguration(PipelineModificationsBuilder userDefinedBehaviors)
        {
            this.userDefinedBehaviors = userDefinedBehaviors;
        }

        public void RegisterFeatureBehaviors(Type featureType, PipelineModificationsBuilder registrations)
        {
            featureBehaviorsRegistrations.Add(new FeatureBehaviorsRegistration(featureType, registrations));
        }

        public RegisterStep ReceiveBehavior { get; set; }

        public PipelineModifications CreateSatellitePipeline(Satellite satellite)
        {
            var composer = new PipelineModificationsComposer();
            foreach (var registration in featureBehaviorsRegistrations.Where(x => satellite.IsEnabled(x.FeatureType)))
            {
                composer.AddSource(registration.Registration);
            }
            composer.AddSource(satellite.SpecificFeaturesRegistration);
            return composer.Compose();
        }

        public PipelineModifications CreateMainPipeline()
        {
            var composer = new PipelineModificationsComposer();
            foreach (var registration in featureBehaviorsRegistrations)
            {
                composer.AddSource(registration.Registration);
            }
            composer.AddSource(userDefinedBehaviors);
            return composer.Compose();
        }

        public void RegisterBehaviorsInContainer(SettingsHolder settings, IConfigureComponents container)
        {
            if (ReceiveBehavior != null)
            {
                ReceiveBehavior.ApplyContainerRegistration(settings, container);
            }
            RegisterBehaviorsInContainer(CreateMainPipeline(), settings, container);
        }

        static void RegisterBehaviorsInContainer(PipelineModifications pipeline, SettingsHolder settings, IConfigureComponents container)
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