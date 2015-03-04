namespace NServiceBus.Features
{
    using NServiceBus.ObjectBuilder;

    /// <summary>
    /// Running everything on the calling thread for now. Features shouldn't be doing anything heavy inside the start and stop.
    /// </summary>
    class FeatureRunner
    {
        readonly FeatureActivator featureActivator;
        readonly IBuilder builder;

        public FeatureRunner(IBuilder builder, FeatureActivator featureActivator)
        {
            this.builder = builder;
            this.featureActivator = featureActivator;
        }

        public void Start()
        {
            featureActivator.StartFeatures(builder);
        }

        public void Stop()
        {
            featureActivator.StopFeatures(builder);
        }
    }
}