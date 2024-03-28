namespace NServiceBus;

using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System;
using Features;

/// <summary>
/// MessagingMetricsFeature captures messaging metrics
/// </summary>
class MessagingMetricsFeature : Feature
{
    public MessagingMetricsFeature() => Prerequisite(c => !c.Receiving.IsSendOnlyEndpoint, "Processing metrics are not supported on send-only endpoints");

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

        context.Pipeline.OnReceivePipelineCompleted((e, _) =>
        {
            e.ProcessedMessage.Headers.TryGetValue(Headers.EnclosedMessageTypes, out var messageTypes);

            var tags = new TagList(new KeyValuePair<string, object>[]
            {
                    new(MeterTags.EndpointDiscriminator, discriminator ?? ""),
                    new(MeterTags.QueueName, queueNameBase ?? ""),
                    new(MeterTags.MessageType, messageTypes ?? "")
            });

            Meters.ProcessingTime.Record((e.CompletedAt - e.StartedAt).TotalMilliseconds, tags);

            if (e.TryGetDeliverAt(out DateTimeOffset startTime) || e.TryGetTimeSent(out startTime))
            {
                Meters.CriticalTime.Record((e.CompletedAt - startTime).TotalMilliseconds, tags);
            }

            return Task.CompletedTask;
        });
    }
}