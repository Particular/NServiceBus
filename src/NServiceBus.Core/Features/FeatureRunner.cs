namespace NServiceBus.Features
{
    using System.Threading.Tasks;
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

        public Task StartAsync(IBusContext busContext)
        {
            return featureActivator.StartFeatures(builder, busContext);
        }

        public Task StopAsync(IBusContext busContext)
        {
            return featureActivator.StopFeatures(builder, busContext);
        }
    }
}