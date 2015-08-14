namespace NServiceBus.Pipeline
{
    using System;

    class FeatureBehaviorsRegistration
    {
        readonly Type featureType;
        readonly PipelineModificationsBuilder registration;

        public FeatureBehaviorsRegistration(Type featureType, PipelineModificationsBuilder registration)
        {
            this.featureType = featureType;
            this.registration = registration;
        }

        public Type FeatureType
        {
            get { return featureType; }
        }

        public PipelineModificationsBuilder Registration
        {
            get { return registration; }
        }
    }
}