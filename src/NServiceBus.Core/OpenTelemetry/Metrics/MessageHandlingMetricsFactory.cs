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
        var handlerType = context.MessageHandler.Instance?.GetType().FullName;
        var tagList = new TagList(new KeyValuePair<string, object>[]
        {
            new(MeterTags.QueueName, queueName ?? ""),
            new(MeterTags.EndpointDiscriminator, discriminator ?? ""),
            new(MeterTags.MessageType, messageType ?? ""),
            new(MeterTags.MessageHandlerType, handlerType ?? "")
        }.AsSpan());
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
        // This is what Add(string, object) does so skipping an unnecessary stack frame
        tags.Add(new KeyValuePair<string, object>(MeterTags.ExecutionResult, "success"));
        Meters.MessageHandlerTime.Record(stopWatch.Elapsed.TotalSeconds, tags);
    }

    public void OnFailure(Exception error)
    {
        stopWatch.Stop();
        tags.Add(new KeyValuePair<string, object>(MeterTags.ExecutionResult, "failure"));
        tags.Add(new KeyValuePair<string, object>(MeterTags.ErrorType, error.GetType().FullName));
        Meters.MessageHandlerTime.Record(stopWatch.Elapsed.TotalSeconds, tags);
    }

    readonly Stopwatch stopWatch = new();
    TagList tags;
}