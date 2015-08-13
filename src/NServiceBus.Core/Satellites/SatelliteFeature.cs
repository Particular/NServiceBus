namespace NServiceBus
{
    using NServiceBus.Features;

    class SatelliteFeature : Feature
    {
        public SatelliteFeature()
        {
            EnableByDefault();
        }

        protected internal override void Setup(FeatureConfigurationContext context)
        {
            context.Pipeline.RegisterConnector<SatelliteToTransportConnector>("Starts the satellite pipeline");
        }
    }
}