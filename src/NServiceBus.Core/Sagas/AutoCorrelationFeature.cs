namespace NServiceBus.Features
{
    using System.Collections.Generic;

    class AutoCorrelationFeature : Feature
    {
        public AutoCorrelationFeature()
        {
            EnableByDefault();
        }

        protected internal override IReadOnlyCollection<FeatureStartupTask> Setup(FeatureConfigurationContext context)
        {
            context.Pipeline.Register("PopulateAutoCorrelationHeadersForReplies", typeof(PopulateAutoCorrelationHeadersForRepliesBehavior), "Copies existing saga headers from incoming message to outgoing message to facilitate the auto correlation in the saga, when replying to a message that was sent by a saga.");

            return FeatureStartupTask.None;
        }
    }
}