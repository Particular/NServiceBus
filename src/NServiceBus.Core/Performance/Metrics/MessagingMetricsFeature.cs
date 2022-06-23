namespace NServiceBus
{
    using System;
    using Features;

    /// <summary>
    /// MessagingMetricsFeature captures messaging metrics
    /// </summary>
    class MessagingMetricsFeature : Feature
    {
        /// <inheritdoc />
        protected internal override void Setup(FeatureConfigurationContext context)
        {
            var isSendOnly = context.Settings.GetOrDefault<bool>("Endpoint.SendOnly");
            if (isSendOnly)
            {
                throw new Exception("Metrics are not supported on send only endpoints.");
            }

            RegisterBehavior(context);
        }

        static void RegisterBehavior(FeatureConfigurationContext context)
        {
            var settings = context.Settings.Get<ReceiveComponent.Settings>();
            var endpointName = settings.EndpointName;
            var discriminator = settings.EndpointInstanceDiscriminator;
            var queueNameBase = settings.CustomQueueNameBase ?? endpointName;

            var performanceDiagnosticsBehavior = new ReceiveDiagnosticsBehavior(endpointName, queueNameBase, discriminator);

            context.Pipeline.Register(
                "NServiceBus.ReceiveDiagnosticsBehavior",
                performanceDiagnosticsBehavior,
                "Provides OpenTelemetry counters for message processing"
            );
        }
    }
}