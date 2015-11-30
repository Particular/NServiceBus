namespace NServiceBus.Features
{
    using System.Threading.Tasks;
    using NServiceBus.ObjectBuilder;

    class FeatureRunner
    {
        FeatureActivator featureActivator;

        public FeatureRunner(FeatureActivator featureActivator)
        {
            this.featureActivator = featureActivator;
        }

        public Task Start(IBuilder builder, IBusContext busContext)
        {
            return featureActivator.StartFeatures(builder, busContext);
        }

        public Task Stop(IBusContext busContext)
        {
            return featureActivator.StopFeatures(busContext);
        }
    }
}