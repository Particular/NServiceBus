namespace NServiceBus;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Pipeline;

class HandlingMetricsFactory(string queueName/*, string discriminator*/) : IHandlingMetricsFactory
{
    public IHandlingMetrics StartHandling(IInvokeHandlerContext context)
    {
        var handlerType = context.MessageHandler.Instance?.GetType().FullName;
        var messageType = context.MessageBeingHandled?.GetType().FullName;
        return new RecordHandlingMetric(new(
        [
            new(MeterTags.QueueName, queueName ?? ""),
            // new(MeterTags.EndpointDiscriminator, discriminator ?? ""),
            new(MeterTags.MessageHandlerType, handlerType),
            new(MeterTags.MessageType, messageType)
        ]));
    }
}

class RecordHandlingMetric : IHandlingMetrics
{
    public RecordHandlingMetric(TagList tags)
    {
        this.tags = tags;
        stopWatch.Start();
    }

    public void OnSuccess()
    {
        stopWatch.Stop();
        Meters.HandlingTime.Record(stopWatch.ElapsedMilliseconds, tags);
    }

    public void OnFailure(Exception error)
    {
        stopWatch.Stop();
        tags.Add(new KeyValuePair<string, object>(MeterTags.FailureType, error.GetType().FullName));
        Meters.HandlingTime.Record(stopWatch.ElapsedMilliseconds, tags);
    }

    readonly Stopwatch stopWatch = new();
    TagList tags;
}