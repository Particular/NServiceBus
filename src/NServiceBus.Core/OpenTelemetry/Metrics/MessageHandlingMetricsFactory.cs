namespace NServiceBus;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Pipeline;

class MessageHandlingMetricsFactory(string queueName, string discriminator) : IMessageHandlingMetricsFactory
{
    public IMessageHandlingMetrics StartHandling(IInvokeHandlerContext context)
    {
        var messageType = context.MessageBeingHandled?.GetType().FullName;
        TagList tagList = MeterTags.CommonMessagingMetricTags(queueName, discriminator, messageType);
        var handlerType = context.MessageHandler.Instance?.GetType().FullName;
        tagList.Add(MeterTags.MessageHandlerType, handlerType ?? "");
        return new RecordMessageHandlingMetric(tagList);
    }
}

class RecordMessageHandlingMetric : IMessageHandlingMetrics
{
    public RecordMessageHandlingMetric(TagList tags)
    {
        this.tags = tags;
        stopWatch.Start();
    }

    public void OnSuccess()
    {
        stopWatch.Stop();
        Meters.MessageHandlerTime.Record(stopWatch.Elapsed.TotalSeconds, tags);
    }

    public void OnFailure(Exception error)
    {
        stopWatch.Stop();
        tags.Add(new KeyValuePair<string, object>(MeterTags.FailureType, error.GetType().FullName));
        Meters.MessageHandlerTime.Record(stopWatch.Elapsed.TotalSeconds, tags);
    }

    readonly Stopwatch stopWatch = new();
    TagList tags;
}