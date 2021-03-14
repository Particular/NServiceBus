namespace NServiceBus
{
    using System.Threading.Tasks;
    using System.Threading;
    using Features;

    class ReceiveStatisticsFeature : Feature
    {
        public ReceiveStatisticsFeature()
        {
            EnableByDefault();
        }

        protected internal override Task Setup(FeatureConfigurationContext context, CancellationToken cancellationToken = default)
        {
            context.Pipeline.Register("ProcessingStatistics", new ProcessingStatisticsBehavior(), "Collects timing for ProcessingStarted and adds the state to determine ProcessingEnded");
            context.Pipeline.Register("AuditProcessingStatistics", new AuditProcessingStatisticsBehavior(), "Add ProcessingStarted and ProcessingEnded headers");

            return Task.CompletedTask;
        }
    }
}