namespace NServiceBus
{
    using Features;

    class ReceiveStatisticsFeature : Feature
    {
        public ReceiveStatisticsFeature()
        {
            EnableByDefault();
        }

        protected internal override void Setup(FeatureConfigurationContext context)
        {
            context.Pipeline.Register("ProcessingStatistics", new ProcessingStatisticsBehavior(), "Collects timing for ProcessingStarted and adds the state to determine ProcessingEnded");
            context.Pipeline.Register("AuditProcessingStatistics", new AuditProcessingStatisticsBehavior(), "Add ProcessingStarted and ProcessingEnded headers");
        }
    }
}