namespace NServiceBus
{
    using System;
    using System.Diagnostics.Metrics;
    using Features;

    /// <summary>
    /// MessagingMetricsFeature captures messaging metrics
    /// </summary>
    class MessagingMetricsFeature : Feature
    {
        internal static readonly Meter NServiceBusMeter = new Meter(
            NServiceBusDiagnosticsInfo.InstrumentationName,
            NServiceBusDiagnosticsInfo.InstrumentationVersion);

        /// <inheritdoc />
        protected internal override void Setup(FeatureConfigurationContext context)
        {
            var isSendOnly = context.Receiving.IsSendOnlyEndpoint;
            if (isSendOnly)
            {
                throw new Exception("Metrics are not supported on send only endpoints.");
            }

            RegisterBehavior(context);
        }

        static void RegisterBehavior(FeatureConfigurationContext context)
        {
            var discriminator = context.Receiving.QueueNameBase;
            var queueNameBase = context.Receiving.InstanceSpecificQueueAddress?.Discriminator;
            var performanceDiagnosticsBehavior = new ReceiveDiagnosticsBehavior(queueNameBase, discriminator);

            context.Pipeline.Register(
                performanceDiagnosticsBehavior,
                "Provides OpenTelemetry counters for message processing"
            );
        }
    }
}