namespace NServiceBus
{
    using Features;
    using Transports;

    class Transport : Feature
    {
        public Transport()
        {
            EnableByDefault();
            Defaults(s =>
            {
                var transportInfrastructure = s.Get<TransportInfrastructure>();
                s.SetDefault<TransportAddresses>(new TransportAddresses(transportInfrastructure.ToTransportAddress));
            });
        }

        protected internal override void Setup(FeatureConfigurationContext context)
        {
            var transportAddresses = context.Settings.Get<TransportAddresses>();
            context.Container.ConfigureComponent(b => transportAddresses, DependencyLifecycle.SingleInstance);
        }
    }
}