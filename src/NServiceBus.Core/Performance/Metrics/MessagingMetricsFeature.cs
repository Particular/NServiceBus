namespace NServiceBus.Performance.Metrics
{
    using Features;

    /// <summary>
    /// MessagingMetricsFeature captures messaging metrics
    /// </summary>
    public class MessagingMetricsFeature : Feature
    {
        /// <inheritdoc />
        protected internal override void Setup(FeatureConfigurationContext context)
        {
            context.ThrowIfSendOnly();
            RegisterBehavior(context);
        }

        static void RegisterBehavior(FeatureConfigurationContext context)
        {
            var performanceDiagnosticsBehavior = new ReceiveDiagnosticsBehavior();

            context.Pipeline.Register(
                "NServiceBus.ReceiveDiagnosticsBehavior",
                performanceDiagnosticsBehavior,
                "Provides OpenTelemetry counters for message processing"
            );
        }
    }
}