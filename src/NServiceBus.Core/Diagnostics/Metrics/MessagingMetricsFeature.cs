namespace NServiceBus
{
    using Features;

    /// <summary>
    /// MessagingMetricsFeature captures messaging metrics
    /// </summary>
    class MessagingMetricsFeature : Feature
    {
        static bool HasMetricsListener => Meters.TotalProcessedSuccessfully.Enabled || Meters.TotalFetched.Enabled || Meters.TotalFailures.Enabled;

        public MessagingMetricsFeature()
        {
            EnableByDefault();
            Prerequisite(c => HasMetricsListener, "No subscribers for messaging metrics");
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