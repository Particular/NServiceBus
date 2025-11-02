namespace NServiceBus;

using Features;

sealed class ReceiveStatisticsFeature : Feature
{
    protected override void Setup(FeatureConfigurationContext context)
    {
        context.Pipeline.Register("ProcessingStatistics", new ProcessingStatisticsBehavior(), "Collects timing for ProcessingStarted and adds the state to determine ProcessingEnded");
        context.Pipeline.Register("AuditProcessingStatistics", new AuditProcessingStatisticsBehavior(), "Add ProcessingStarted and ProcessingEnded headers");
    }
}