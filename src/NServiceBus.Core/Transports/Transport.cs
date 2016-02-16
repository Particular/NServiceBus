namespace NServiceBus
{
    using NServiceBus.Features;
    using NServiceBus.Transports;

    class Transport : Feature
    {
        public Transport()
        {
            EnableByDefault();
            DependsOn<UnicastBus>();
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