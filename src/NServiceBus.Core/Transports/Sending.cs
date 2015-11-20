namespace NServiceBus.Transports
{
    using NServiceBus.Features;
    using NServiceBus.OutgoingPipeline;
    using NServiceBus.Pipeline;
    using NServiceBus.TransportDispatch;

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
            context.Container.ConfigureComponent(b => new SendPipeline(b, context.Settings, context.Settings.Get<PipelineConfiguration>().MainPipeline),  DependencyLifecycle.SingleInstance);
            context.Container.ConfigureComponent(b => new PublishPipeline(b, context.Settings, context.Settings.Get<PipelineConfiguration>().MainPipeline),  DependencyLifecycle.SingleInstance);
            context.Container.ConfigureComponent(b => new RoutingPipeline(b, context.Settings, context.Settings.Get<PipelineConfiguration>().MainPipeline),  DependencyLifecycle.SingleInstance);
            context.Container.ConfigureComponent(b => new ReplyPipeline(b, context.Settings, context.Settings.Get<PipelineConfiguration>().MainPipeline),  DependencyLifecycle.SingleInstance);
        }
    }
}