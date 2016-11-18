namespace NServiceBus.Features
{
    class UnicastPublishSubscribe : Feature
    {
        public UnicastPublishSubscribe()
        {
            EnableByDefault();
        }

        protected internal override void Setup(FeatureConfigurationContext context)
        {
            context.Pipeline.Register(b => new UnicastPublishRouterConnector(b.Build<IUnicastPublish>()), "Determines how the published messages should be routed");
        }
    }
}