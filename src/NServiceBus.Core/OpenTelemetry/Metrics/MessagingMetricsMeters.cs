namespace NServiceBus;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using Pipeline;

class MessagingMetricsMeters
{
    public MessagingMetricsMeters(IMeterFactory meterFactory)
    {
        // TODO the problem with this approach is that it's harder to do approval testing on the exposed meters
        // TODO we also probably need to keep the meter around to assert that the version did not change 
        var meter = meterFactory.Create("NServiceBus.Core", "0.2.0");
        totalProcessedSuccessfully = meter.CreateCounter<long>(Metrics.TotalProcessedSuccessfully,
            description: "Total number of messages processed successfully by the endpoint.");
        totalFetched = meter.CreateCounter<long>(Metrics.TotalFetched,
            description: "Total number of messages fetched from the queue by the endpoint.");
        totalFailures = meter.CreateCounter<long>(Metrics.TotalFailures,
            description: "Total number of messages processed unsuccessfully by the endpoint.");
        messageHandlerTime = meter.CreateHistogram<double>(Metrics.MessageHandlerTime, "s",
            "The time in seconds for the execution of the business code.");
        criticalTime = meter.CreateHistogram<double>(Metrics.CriticalTime, "s",
            "The time in seconds between when the message was sent until processed by the endpoint.");
    }

    public void RecordMessageSuccessfullyProcessed(TagList tags) => totalProcessedSuccessfully.Add(1, tags);
    public void RecordMessageProcessingFailure(TagList tags) => totalFailures.Add(1, tags);
    public void RecordFetchedMessage(TagList tags) => totalFetched.Add(1, tags);
    public void RecordMessageCriticalTime(TimeSpan messageCriticalTime, TagList tags) => criticalTime.Record(messageCriticalTime.TotalSeconds, tags);

    public void RecordSuccessfulMessageHandlerTime(IInvokeHandlerContext invokeHandlerContext, TimeSpan elapsed)
    {
        // TODO if there was a provider/factory style approach to getting the MessagingMetricsMeters
        // we could inject a NoOpMessagingMetricsMeters and get rid of all these checks
        if (!invokeHandlerContext.Extensions.TryGet<IncomingPipelineMetricTags>(out var incomingPipelineMetricTags) || incomingPipelineMetricTags is not { IsMetricTagsCollectionEnabled: true })
        {
            return;
        }

        TagList meterTags;
        incomingPipelineMetricTags.ApplyTags(ref meterTags, [MeterTags.QueueName,
            MeterTags.EndpointDiscriminator,
            MeterTags.MessageType,
            MeterTags.MessageHandlerType]);
        // This is what Add(string, object) does so skipping an unnecessary stack frame
        meterTags.Add(new KeyValuePair<string, object>(MeterTags.MessageHandlerType, invokeHandlerContext.MessageHandler.HandlerType.FullName));
        meterTags.Add(new KeyValuePair<string, object>(MeterTags.ExecutionResult, "success"));
        messageHandlerTime.Record(elapsed.TotalSeconds, meterTags);
    }

    public void RecordFailedMessageHandlerTime(IInvokeHandlerContext invokeHandlerContext, TimeSpan elapsed, Exception error)
    {
        if (!invokeHandlerContext.Extensions.TryGet<IncomingPipelineMetricTags>(out var incomingPipelineMetricTags) || incomingPipelineMetricTags is not { IsMetricTagsCollectionEnabled: true })
        {
            return;
        }

        TagList meterTags;
        incomingPipelineMetricTags.ApplyTags(ref meterTags, [MeterTags.QueueName,
            MeterTags.EndpointDiscriminator,
            MeterTags.MessageType,
            MeterTags.MessageHandlerType]);
        // This is what Add(string, object) does so skipping an unnecessary stack frame
        meterTags.Add(new KeyValuePair<string, object>(MeterTags.MessageHandlerType, invokeHandlerContext.MessageHandler.HandlerType.FullName));
        meterTags.Add(new KeyValuePair<string, object>(MeterTags.ExecutionResult, "failure"));
        meterTags.Add(new KeyValuePair<string, object>(MeterTags.ErrorType, error.GetType().FullName));
        messageHandlerTime.Record(elapsed.TotalSeconds, meterTags);
    }

    readonly Counter<long> totalProcessedSuccessfully;
    readonly Counter<long> totalFetched;
    readonly Counter<long> totalFailures;
    readonly Histogram<double> messageHandlerTime;
    readonly Histogram<double> criticalTime;
}