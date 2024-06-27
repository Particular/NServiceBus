namespace NServiceBus;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using Pipeline;
using Settings;

class IncomingPipelineMetrics
{
    const string TotalProcessedSuccessfully = "nservicebus.messaging.successes";
    const string TotalFetched = "nservicebus.messaging.fetches";
    const string TotalFailures = "nservicebus.messaging.failures";
    const string MessageHandlerTime = "nservicebus.messaging.handler_time";
    const string CriticalTime = "nservicebus.messaging.critical_time";

    public IncomingPipelineMetrics(IMeterFactory meterFactory, IReadOnlySettings settings)
    {
        var meter = meterFactory.Create("NServiceBus.Core.Pipeline.Incoming", "0.2.0");
        totalProcessedSuccessfully = meter.CreateCounter<long>(TotalProcessedSuccessfully,
            description: "Total number of messages processed successfully by the endpoint.");
        totalFetched = meter.CreateCounter<long>(TotalFetched,
            description: "Total number of messages fetched from the queue by the endpoint.");
        totalFailures = meter.CreateCounter<long>(TotalFailures,
            description: "Total number of messages processed unsuccessfully by the endpoint.");
        messageHandlerTime = meter.CreateHistogram<double>(MessageHandlerTime, "s",
            "The time in seconds for the execution of the business code.");
        criticalTime = meter.CreateHistogram<double>(CriticalTime, "s",
            "The time in seconds between when the message was sent until processed by the endpoint.");

        var config = settings.Get<ReceiveComponent.Configuration>();
        queueNameBase = config.QueueNameBase;
        endpointDiscriminator = config.InstanceSpecificQueueAddress?.Discriminator ?? "";
    }

    public IncomingPipelineMetricTags CreateDefaultIncomingPipelineMetricTags()
    {
        var incomingPipelineMetricsTags = new IncomingPipelineMetricTags();
        incomingPipelineMetricsTags.Add(MeterTags.QueueName, queueNameBase);
        incomingPipelineMetricsTags.Add(MeterTags.EndpointDiscriminator, endpointDiscriminator ?? "");

        return incomingPipelineMetricsTags;
    }

    public void RecordMessageSuccessfullyProcessed(IncomingPipelineMetricTags incomingPipelineMetricTags)
    {
        if (!totalProcessedSuccessfully.Enabled)
        {
            return;
        }

        TagList tags;
        incomingPipelineMetricTags.ApplyTags(ref tags, [
            MeterTags.MessageType,
            MeterTags.MessageHandlerTypes]);

        totalProcessedSuccessfully.Add(1, tags);
    }

    public void RecordMessageProcessingFailure(IncomingPipelineMetricTags incomingPipelineMetricTags, Exception error)
    {
        if (!totalFailures.Enabled)
        {
            return;
        }

        TagList tags;
        tags.Add(new(MeterTags.FailureType, error.GetType()));
        incomingPipelineMetricTags.ApplyTags(ref tags, [
            MeterTags.MessageType,
            MeterTags.MessageHandlerTypes]);
        totalFailures.Add(1, tags);
    }

    public void RecordFetchedMessage(IncomingPipelineMetricTags incomingPipelineMetricTags)
    {
        if (!totalFetched.Enabled)
        {
            return;
        }

        TagList tags;
        incomingPipelineMetricTags.ApplyTags(ref tags, [
            MeterTags.EndpointDiscriminator,
            MeterTags.QueueName]);

        totalFetched.Add(1, tags);
    }

    public void RecordMessageCriticalTime(TimeSpan messageCriticalTime, IncomingPipelineMetricTags incomingPipelineMetricTags)
    {
        if (!criticalTime.Enabled)
        {
            return;
        }

        TagList tags;
        incomingPipelineMetricTags.ApplyTags(ref tags, [
            MeterTags.QueueName,
            MeterTags.EndpointDiscriminator,
            MeterTags.MessageType]);

        criticalTime.Record(messageCriticalTime.TotalSeconds, tags);
    }

    public void RecordSuccessfulMessageHandlerTime(IInvokeHandlerContext invokeHandlerContext, TimeSpan elapsed)
    {
        if (!messageHandlerTime.Enabled)
        {
            return;
        }

        var incomingPipelineMetricTags = invokeHandlerContext.Extensions.Get<IncomingPipelineMetricTags>();
        TagList meterTags;
        incomingPipelineMetricTags.ApplyTags(ref meterTags, [
            MeterTags.QueueName,
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
        if (!messageHandlerTime.Enabled)
        {
            return;
        }

        var incomingPipelineMetricTags = invokeHandlerContext.Extensions.Get<IncomingPipelineMetricTags>();
        TagList meterTags;
        incomingPipelineMetricTags.ApplyTags(ref meterTags, [
            MeterTags.QueueName,
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
    string queueNameBase;
    string endpointDiscriminator;
}