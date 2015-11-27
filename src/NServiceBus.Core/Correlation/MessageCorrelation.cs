namespace NServiceBus.Features
{
    using System.Collections.Generic;

    class MessageCorrelation : Feature
    {
        public MessageCorrelation()
        {
            EnableByDefault();
        }
        protected internal override IReadOnlyCollection<FeatureStartupTask> Setup(FeatureConfigurationContext context)
        {
            context.Pipeline.Register("AttachCorrelationId", typeof(AttachCorrelationIdBehavior), "Makes sure that outgoing messages have a correlation id header set");

            return FeatureStartupTask.None;
        }
    }
}