namespace NServiceBus;

using System.Threading;
using System.Threading.Tasks;

class CriticalTimeMetrics(string queueName, string discriminator)
{
    public Task Record(ReceivePipelineCompleted pipeline, CancellationToken cancellationToken)
    {
        pipeline.TryGetMessageType(out var messageType);
        var tags = MeterTags.CommonMessagingMetricTags(queueName, discriminator, messageType);

        if (pipeline.TryGetDeliverAt(out var startTime) || pipeline.TryGetTimeSent(out startTime))
        {
            Meters.CriticalTime.Record((pipeline.CompletedAt - startTime).TotalMilliseconds, tags);
        }
        return Task.CompletedTask;
    }
}