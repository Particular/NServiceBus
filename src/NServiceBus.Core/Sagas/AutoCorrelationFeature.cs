namespace NServiceBus.Features
{
    class AutoCorrelationFeature : Feature
    {
        public AutoCorrelationFeature()
        {
            EnableByDefault();
        }

        protected internal override void Setup(FeatureConfigurationContext context)
        {
            context.Pipeline.Register("PopulateAutoCorrelationHeadersForReplies", new PopulateAutoCorrelationHeadersForRepliesBehavior(), "Copies existing saga headers from incoming message to outgoing message to facilitate the auto correlation in the saga, when replying to a message that was sent by a saga.");
        }
    }
}