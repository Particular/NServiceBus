namespace NServiceBus.Features
{
    using NServiceBus.ObjectBuilder;

    /// <summary>
    /// Running everything on the calling thread for now. Features shouldn't be doing anything heavy inside the start and stop.
    /// </summary>
    class FeatureRunner
    {
        FeatureActivator featureActivator;
        IBuilder builder;

        public FeatureRunner(IBuilder builder, FeatureActivator featureActivator)
        {
            this.builder = builder;
            this.featureActivator = featureActivator;
        }

        public void Start(ISendOnlyBus sendOnlyBus)
        {
            featureActivator.StartFeatures(builder, sendOnlyBus);
        }

        public void Stop()
        {
            featureActivator.StopFeatures(builder);
        }
    }
}