namespace NServiceBus
{
    using System.Threading.Tasks;
    using Features;

    /// <summary>
    /// Provides performance counters for receive operations.
    /// </summary>
    public class ReceiveStatisticsPerformanceCounterFeature : Feature
    {
        internal ReceiveStatisticsPerformanceCounterFeature()
        {
            EnableByDefault();
        }

        /// <summary>
        /// See <see cref="Feature.Setup" />.
        /// </summary>
        protected internal override void Setup(FeatureConfigurationContext context)
        {
            var logicalAddress = context.Settings.LogicalAddress();
            var performanceDiagnosticsBehavior = new ReceivePerformanceDiagnosticsBehavior(logicalAddress.EndpointInstance.Endpoint);

            context.Pipeline.Register(performanceDiagnosticsBehavior, "Provides various performance counters for receive statistics");

            context.RegisterStartupTask(new WarmupCooldownTask(performanceDiagnosticsBehavior));
        }

        class WarmupCooldownTask : FeatureStartupTask
        {
            public WarmupCooldownTask(ReceivePerformanceDiagnosticsBehavior behavior)
            {
                this.behavior = behavior;
            }

            protected override Task OnStart(IMessageSession session)
            {
                behavior.Warmup();
                return TaskEx.CompletedTask;
            }

            protected override Task OnStop(IMessageSession session)
            {
                behavior.Cooldown();
                return TaskEx.CompletedTask;
            }

            readonly ReceivePerformanceDiagnosticsBehavior behavior;
        }
    }
}