namespace NServiceBus;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Pipeline;

class RecordMessageHandlingMetric
{
    public RecordMessageHandlingMetric(IInvokeHandlerContext context)
    {
        this.context = context;
        incomingPipelineMetricTags = context.Extensions.Get<IncomingPipelineMetricTags>();
        if (incomingPipelineMetricTags.IsMetricTagsCollectionEnabled)
        {
            stopWatch.Start();
        }
    }

    public void OnSuccess()
    {
        if (!incomingPipelineMetricTags.IsMetricTagsCollectionEnabled)
        {
            return;
        }

        stopWatch.Stop();

        incomingPipelineMetricTags.ApplyTags(ref tags, [MeterTags.QueueName,
            MeterTags.EndpointDiscriminator,
            MeterTags.MessageType,
            MeterTags.MessageHandlerType]);
        // This is what Add(string, object) does so skipping an unnecessary stack frame
        tags.Add(new KeyValuePair<string, object>(MeterTags.MessageHandlerType, context.MessageHandler.Instance.GetType().FullName));
        tags.Add(new KeyValuePair<string, object>(MeterTags.ExecutionResult, "success"));
        Meters.MessageHandlerTime.Record(stopWatch.Elapsed.TotalSeconds, tags);
    }

    public void OnFailure(Exception error)
    {
        if (!incomingPipelineMetricTags.IsMetricTagsCollectionEnabled)
        {
            return;
        }

        stopWatch.Stop();

        incomingPipelineMetricTags.ApplyTags(ref tags, [MeterTags.QueueName,
            MeterTags.EndpointDiscriminator,
            MeterTags.MessageType,
            MeterTags.MessageHandlerType]);
        tags.Add(new KeyValuePair<string, object>(MeterTags.MessageHandlerType, context.MessageHandler.Instance.GetType().FullName));
        tags.Add(new KeyValuePair<string, object>(MeterTags.ExecutionResult, "failure"));
        tags.Add(new KeyValuePair<string, object>(MeterTags.ErrorType, error.GetType().FullName));
        Meters.MessageHandlerTime.Record(stopWatch.Elapsed.TotalSeconds, tags);
    }

    readonly Stopwatch stopWatch = new();
    TagList tags;
    readonly IncomingPipelineMetricTags incomingPipelineMetricTags;
    readonly IInvokeHandlerContext context;
}