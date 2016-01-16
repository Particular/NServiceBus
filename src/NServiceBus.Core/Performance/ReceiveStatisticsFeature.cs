namespace NServiceBus
{
    using System.Threading.Tasks;
    using NServiceBus.Features;

    class ReceiveStatisticsFeature : Feature
    {
        public ReceiveStatisticsFeature()
        {
            EnableByDefault();
        }
        protected internal override void Setup(FeatureConfigurationContext context)
        {
            var performanceDiagnosticsBehavior = new ReceivePerformanceDiagnosticsBehavior(context.Settings.LocalAddress());

            context.Pipeline.Register("ReceivePerformanceDiagnosticsBehavior", performanceDiagnosticsBehavior, "Provides various performance counters for receive statistics");
            context.Pipeline.Register<ProcessingStatisticsBehavior.Registration>();
            context.Pipeline.Register("AuditProcessingStatistics", new AuditProcessingStatisticsBehavior(), "Add ProcessingStarted and ProcessingEnded headers");

            context.RegisterStartupTask(new WarmupCooldownTask(performanceDiagnosticsBehavior));
        }

        class WarmupCooldownTask : FeatureStartupTask
        {
            readonly ReceivePerformanceDiagnosticsBehavior behavior;

            public WarmupCooldownTask(ReceivePerformanceDiagnosticsBehavior behavior)
            {
                this.behavior = behavior;
            }

            protected override Task OnStart(IBusSession session)
            {
                behavior.Warmup();
                return TaskEx.CompletedTask;
            }

            protected override Task OnStop(IBusSession session)
            {
                behavior.Cooldown();
                return TaskEx.CompletedTask;
            }
        }
    }
}