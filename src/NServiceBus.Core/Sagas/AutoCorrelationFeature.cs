namespace NServiceBus.Features
{
    using System.Threading.Tasks;
    using System.Threading;

    class AutoCorrelationFeature : Feature
    {
        public AutoCorrelationFeature()
        {
            EnableByDefault();
        }

        protected internal override Task Setup(FeatureConfigurationContext context, CancellationToken cancellationToken = default)
        {
            context.Pipeline.Register("PopulateAutoCorrelationHeadersForReplies", new PopulateAutoCorrelationHeadersForRepliesBehavior(), "Copies existing saga headers from incoming message to outgoing message to facilitate the auto correlation in the saga, when replying to a message that was sent by a saga.");
            return Task.CompletedTask;
        }
    }
}