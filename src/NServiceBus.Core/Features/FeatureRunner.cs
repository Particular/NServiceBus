namespace NServiceBus.Features
{
    using System.Threading.Tasks;
    using ObjectBuilder;

    class FeatureRunner
    {
        public FeatureRunner(FeatureActivator featureActivator)
        {
            this.featureActivator = featureActivator;
        }

        public Task Start(IBuilder builder, IMessageSession messageSession)
        {
            return featureActivator.StartFeatures(builder, messageSession);
        }

        public Task Stop(IMessageSession messageSession)
        {
            return featureActivator.StopFeatures(messageSession);
        }

        FeatureActivator featureActivator;
    }
}