namespace NServiceBus.Features
{
    using NServiceBus.Routing;
    using NServiceBus.Settings;
    using NServiceBus.Transports;

    class UnicastBus : Feature
    {
        internal UnicastBus()
        {
            EnableByDefault();
            Defaults(s =>
            {
                var endpointInstanceName = GetEndpointInstanceName(s);
                var rootLogicalAddress = new LogicalAddress(endpointInstanceName);
                s.SetDefault<EndpointInstance>(endpointInstanceName);
                s.SetDefault<LogicalAddress>(rootLogicalAddress);
            });
        }

        static EndpointInstance GetEndpointInstanceName(ReadOnlySettings settings)
        {
            var userDiscriminator = settings.GetOrDefault<string>("EndpointInstanceDiscriminator");
            var boundInstance = settings.Get<TransportDefinition>().BindToLocalEndpoint(new EndpointInstance(settings.EndpointName(), userDiscriminator), settings);
            return boundInstance;
        }

        protected internal override void Setup(FeatureConfigurationContext context)
        {
            context.Container.ConfigureComponent<BusNotifications>(DependencyLifecycle.SingleInstance);
        }
    }
}