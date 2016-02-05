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

        public Task Start(IChildBuilder builder, IBusSession busSession)
        {
            return featureActivator.StartFeatures(builder, busSession);
        }

        public Task Stop(IBusSession busSession)
        {
            return featureActivator.StopFeatures(busSession);
        }
    }
}