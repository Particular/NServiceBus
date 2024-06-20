namespace NServiceBus;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

class CriticalTimeMetrics(string queueName, string discriminator)
{
    public Task Record(ReceivePipelineCompleted pipeline, CancellationToken cancellationToken = default)
    {
        _ = pipeline.TryGetMessageType(out var messageType);
        var tagList = new TagList(new KeyValuePair<string, object>[]
        {
            new(MeterTags.QueueName, queueName ?? ""),
            new(MeterTags.EndpointDiscriminator, discriminator ?? ""),
            new(MeterTags.MessageType, messageType ?? "")
        }.AsSpan());

        if (pipeline.TryGetDeliverAt(out var startTime) || pipeline.TryGetTimeSent(out startTime))
        {
            Meters.CriticalTime.Record((pipeline.CompletedAt - startTime).TotalMilliseconds, tagList);
        }
        return Task.CompletedTask;
    }
}