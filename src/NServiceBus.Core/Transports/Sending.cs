namespace NServiceBus.Transports
{
    using System.Collections.Generic;
    using NServiceBus.Features;

    class Sending : Feature
    {
        public Sending()
        {
            EnableByDefault();
            DependsOn<UnicastBus>();
        }

        protected internal override IReadOnlyCollection<FeatureStartupTask> Setup(FeatureConfigurationContext context)
        {
            var transport = context.Settings.Get<OutboundTransport>();
            context.Container.ConfigureComponent(c =>
            {
                var sendConfigContext = transport.Configure(context.Settings);
                var dispatcher = sendConfigContext.DispatcherFactory();
                return dispatcher;
            }, DependencyLifecycle.SingleInstance);

            return FeatureStartupTask.None;
        }
    }
}