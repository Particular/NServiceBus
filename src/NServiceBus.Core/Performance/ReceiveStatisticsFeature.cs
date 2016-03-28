namespace NServiceBus
{
    using Features;
    using Pipeline;

    class ReceiveStatisticsFeature : Feature
    {
        public ReceiveStatisticsFeature()
        {
            EnableByDefault();
        }

        protected internal override void Setup(FeatureConfigurationContext context)
        {

            context.Pipeline.Register(WellKnownStep.ProcessingStatistics, new ProcessingStatisticsBehavior(), "Collects timing for ProcessingStarted and adds the state to determine ProcessingEnded");
            context.Pipeline.Register("AuditProcessingStatistics", new AuditProcessingStatisticsBehavior(), "Add ProcessingStarted and ProcessingEnded headers");

        }
    }
}