namespace NServiceBus.Features
{
    class MessageCorrelation : Feature
    {
        public MessageCorrelation()
        {
            EnableByDefault();
        }

        protected internal override void Setup(FeatureConfigurationContext context)
        {
            context.Pipeline.Register("AttachCorrelationId", new AttachCorrelationIdBehavior(), "Makes sure that outgoing messages have a correlation id header set");
        }
    }
}