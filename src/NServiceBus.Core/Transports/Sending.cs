namespace NServiceBus.Transports
{
    using NServiceBus.Features;

    class Sending : Feature
    {
        public Sending()
        {
            EnableByDefault();
            DependsOn<UnicastBus>();
        }

        protected internal override void Setup(FeatureConfigurationContext context)
        {
            var transport = context.Settings.Get<OutboundTransport>();
            context.Container.ConfigureComponent(c =>
            {
                var sendConfigContext = transport.Configure(context.Settings);
                var dispatcher = sendConfigContext.DispatcherFactory();
                return dispatcher;
            }, DependencyLifecycle.SingleInstance);
        }
    }
}