namespace NServiceBus;

using System;
using System.Collections.Generic;
using System.Diagnostics;

class CriticalTimeMetrics(string queueName, string discriminator)
{
    public void Record(ReceivePipelineCompleted pipeline)
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
            Meters.CriticalTime.Record((pipeline.CompletedAt - startTime).TotalSeconds, tagList);
        }
    }
}