namespace NServiceBus.Features
{
    using System.Threading.Tasks;
    using System.Threading;

    class MessageCorrelation : Feature
    {
        public MessageCorrelation()
        {
            EnableByDefault();
        }

        protected internal override Task Setup(FeatureConfigurationContext context, CancellationToken cancellationToken = default)
        {
            context.Pipeline.Register("AttachCorrelationId", new AttachCorrelationIdBehavior(), "Makes sure that outgoing messages have a correlation id header set");
            return Task.CompletedTask;
        }
    }
}