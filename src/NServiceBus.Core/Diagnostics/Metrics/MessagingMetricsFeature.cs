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

        internal static readonly Counter<long> TotalProcessedSuccessfully =
            NServiceBusMeter.CreateCounter<long>("nservicebus.messaging.successes", description: "Total number of messages processed successfully by the endpoint.");

        internal static readonly Counter<long> TotalFetched =
            NServiceBusMeter.CreateCounter<long>("nservicebus.messaging.fetches", description: "Total number of messages fetched from the queue by the endpoint.");

        internal static readonly Counter<long> TotalFailures =
            NServiceBusMeter.CreateCounter<long>("nservicebus.messaging.failures", description: "Total number of messages processed unsuccessfully by the endpoint.");

        public MessagingMetricsFeature()
        {
            EnableByDefault();
            Prerequisite(c => TotalProcessedSuccessfully.Enabled || TotalFetched.Enabled || TotalFailures.Enabled, "No subscribers for messaging metrics");
            Prerequisite(c => !c.Receiving.IsSendOnlyEndpoint, "Processing metrics are not supported on send-only endpoints");
        }

        /// <inheritdoc />
        protected internal override void Setup(FeatureConfigurationContext context)
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