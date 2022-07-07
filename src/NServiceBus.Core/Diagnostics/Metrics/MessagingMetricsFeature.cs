namespace NServiceBus
{
    using System.Diagnostics.Metrics;
    using Features;

    /// <summary>
    /// MessagingMetricsFeature captures messaging metrics
    /// </summary>
    class MessagingMetricsFeature : Feature
    {
        internal static readonly Meter NServiceBusMeter = new Meter(
            "NServiceBus.Core",
            "0.1.0");

        public MessagingMetricsFeature() => Prerequisite(c => !c.Receiving.IsSendOnlyEndpoint, "Processing metrics are not supported on send-only endpoints");

        /// <inheritdoc />
        protected internal override void Setup(FeatureConfigurationContext context) => RegisterBehavior(context);

        static void RegisterBehavior(FeatureConfigurationContext context)
        {
            var discriminator = context.Receiving.InstanceSpecificQueueAddress?.Discriminator;
            var queueNameBase = context.Receiving.QueueNameBase;
            var performanceDiagnosticsBehavior = new ReceiveDiagnosticsBehavior(queueNameBase, discriminator);

            context.Pipeline.Register(
                performanceDiagnosticsBehavior,
                "Provides OpenTelemetry counters for message processing"
            );
        }
    }
}