namespace NServiceBus;

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Pipeline;

// This behavior is IIncomingPhysicalMessageContext and not ITransportReceiveContext
// to avoid capture successes for messages deduplicated by the Outbox
class ReceiveDiagnosticsBehavior(MessagingMetricsMeters metricsMeters, string queueNameBase, string discriminator)
    : IBehavior<IIncomingPhysicalMessageContext, IIncomingPhysicalMessageContext>
{
    public async Task Invoke(IIncomingPhysicalMessageContext context, Func<IIncomingPhysicalMessageContext, Task> next)
    {
        var availableMetricTags = context.Extensions.Get<IncomingPipelineMetricTags>();
        availableMetricTags.Add(MeterTags.EndpointDiscriminator, discriminator);
        availableMetricTags.Add(MeterTags.QueueName, queueNameBase);

        var tags = new TagList();
        availableMetricTags.ApplyTags(ref tags, [MeterTags.EndpointDiscriminator, MeterTags.QueueName]);
        metricsMeters.RecordFetchedMessage(tags);

        try
        {
            await next(context).ConfigureAwait(false);
        }
        catch (Exception ex) when (!ex.IsCausedBy(context.CancellationToken))
        {
            tags.Add(new(MeterTags.FailureType, ex.GetType()));
            availableMetricTags.ApplyTags(ref tags, [MeterTags.MessageType, MeterTags.MessageHandlerTypes]);
            metricsMeters.RecordMessageProcessingFailure(tags);
            throw;
        }

        availableMetricTags.ApplyTags(ref tags, [MeterTags.MessageType, MeterTags.MessageHandlerTypes]);
        metricsMeters.RecordMessageSuccessfullyProcessed(tags);
    }

    readonly string queueNameBase = queueNameBase;
    readonly string discriminator = discriminator;
}