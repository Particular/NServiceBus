namespace NServiceBus
{
    using System;
    using NServiceBus.Audit;
    using NServiceBus.Features;
    using NServiceBus.Pipeline;

    class ReceiveStatisticsFeature:Feature
    {
        public ReceiveStatisticsFeature()
        {
            EnableByDefault();
        }
        protected internal override void Setup(FeatureConfigurationContext context)
        {
            context.Pipeline.Register("ReceivePerformanceDiagnosticsBehavior", typeof(ReceivePerformanceDiagnosticsBehavior), "Provides various performance counters for receive statistics");
            context.Pipeline.Register<ProcessingStatisticsBehavior.Registration>();
            context.Pipeline.Register("AuditProcessingStatistics", typeof(AuditProcessingStatisticsBehavior), "Add ProcessingStarted and ProcessingEnded headers");
        }
    }


    class AuditProcessingStatisticsBehavior:Behavior<AuditContext>
    {
        public override void Invoke(AuditContext context, Action next)
        {

            ProcessingStatisticsBehavior.State state;

            if (context.TryGet(out state))
            {
                context.AddAuditData(Headers.ProcessingStarted,DateTimeExtensions.ToWireFormattedString(state.ProcessingStarted));
                context.AddAuditData(Headers.ProcessingEnded, DateTimeExtensions.ToWireFormattedString(state.ProcessingEnded));
            }

            next();
        }
    }
}