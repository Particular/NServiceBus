namespace NServiceBus;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

class ProcessingTimeMetrics(string queueName, string discriminator)
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

        Meters.ProcessingTime.Record((pipeline.CompletedAt - pipeline.StartedAt).TotalSeconds, tagList);
        return Task.CompletedTask;
    }
}