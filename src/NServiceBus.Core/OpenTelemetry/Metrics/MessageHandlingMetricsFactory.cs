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
        var tagList = new TagList(new KeyValuePair<string, object>[]
        {
            new(MeterTags.QueueName, queueName ?? ""),
            new(MeterTags.EndpointDiscriminator, discriminator ?? ""),
            new(MeterTags.MessageType, messageType ?? "")
        }.AsSpan());
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
        tags.Add(new KeyValuePair<string, object>(MeterTags.ExecutionResult, "success"));
        PipelineMeters.MessageHandlerTime.Record(stopWatch.Elapsed.TotalSeconds, tags);
    }

    public void OnFailure(Exception error)
    {
        stopWatch.Stop();
        tags.Add(new KeyValuePair<string, object>(MeterTags.ExecutionResult, "failure"));
        tags.Add(new KeyValuePair<string, object>(MeterTags.ErrorType, error.GetType().FullName));
        PipelineMeters.MessageHandlerTime.Record(stopWatch.Elapsed.TotalSeconds, tags);
    }

    readonly Stopwatch stopWatch = new();
    TagList tags;
}