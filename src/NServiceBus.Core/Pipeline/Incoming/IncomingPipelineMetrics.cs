namespace NServiceBus;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using Pipeline;

class IncomingPipelineMetrics
{
    const string TotalProcessedSuccessfully = "nservicebus.messaging.successes";
    const string TotalFetched = "nservicebus.messaging.fetches";
    const string TotalFailures = "nservicebus.messaging.failures";
    const string MessageHandlerTime = "nservicebus.messaging.handler_time";
    const string CriticalTime = "nservicebus.messaging.critical_time";
    const string RecoverabilityImmediate = "nservicebus.recoverability.immediate";
    const string RecoverabilityDelayed = "nservicebus.recoverability.delayed";
    const string RecoverabilityError = "nservicebus.recoverability.error";

    public IncomingPipelineMetrics(IMeterFactory meterFactory, string queueName, string discriminator)
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
        totalImmediateRetries = meter.CreateCounter<long>(RecoverabilityImmediate,
            description: "Total number of immediate retries requested.");
        totalDelayedRetries = meter.CreateCounter<long>(RecoverabilityDelayed,
            description: "Total number of delayed retries requested.");
        totalSentToErrorQueue = meter.CreateCounter<long>(RecoverabilityError,
            description: "Total number of messages sent to the error queue.");

        queueNameBase = queueName;
        endpointDiscriminator = discriminator;
    }

    public void AddDefaultIncomingPipelineMetricTags(IncomingPipelineMetricTags incomingPipelineMetricsTags)
    {
        incomingPipelineMetricsTags.Add(MeterTags.QueueName, queueNameBase);
        incomingPipelineMetricsTags.Add(MeterTags.EndpointDiscriminator, endpointDiscriminator ?? "");
    }

    public void RecordMessageSuccessfullyProcessed(ITransportReceiveContext context, IncomingPipelineMetricTags incomingPipelineMetricTags)
    {
        if (!totalProcessedSuccessfully.Enabled && !criticalTime.Enabled)
        {
            return;
        }

        TagList tags;
        incomingPipelineMetricTags.ApplyTags(ref tags, [
            MeterTags.QueueName,
            MeterTags.EndpointDiscriminator,
            MeterTags.MessageType,
            MeterTags.MessageHandlerTypes]);

        if (totalProcessedSuccessfully.Enabled)
        {
            totalProcessedSuccessfully.Add(1, tags);
        }
        if (criticalTime.Enabled)
        {
            var completedAt = DateTimeOffset.UtcNow;

            if (context.Message.Headers.TryGetDeliverAt(out var startTime)
                || context.Message.Headers.TryGetTimeSent(out startTime))
            {
                var criticalTimeElapsed = completedAt - startTime;
                criticalTime.Record(criticalTimeElapsed.TotalSeconds, tags);
            }
        }
    }

    public void RecordMessageProcessingFailure(IncomingPipelineMetricTags incomingPipelineMetricTags, Exception error)
    {
        if (!totalFailures.Enabled)
        {
            return;
        }

        TagList tags;
        tags.Add(new(MeterTags.FailureType, error.GetType().FullName));
        incomingPipelineMetricTags.ApplyTags(ref tags, [
            MeterTags.QueueName,
            MeterTags.EndpointDiscriminator,
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

    public void RecordImmediateRetry(IRecoverabilityContext recoverabilityContext)
    {
        if (!totalImmediateRetries.Enabled)
        {
            return;
        }

        var incomingPipelineMetricTags = recoverabilityContext.Extensions.Get<IncomingPipelineMetricTags>();
        TagList meterTags;
        incomingPipelineMetricTags.ApplyTags(ref meterTags, [
            MeterTags.QueueName,
            MeterTags.EndpointDiscriminator,
            MeterTags.MessageType,
            MeterTags.MessageHandlerType]);
        // This is what Add(string, object) does so skipping an unnecessary stack frame
        meterTags.Add(new KeyValuePair<string, object>(MeterTags.ErrorType, recoverabilityContext.Exception.GetType().FullName));
        totalImmediateRetries.Add(1, meterTags);
    }

    public void RecordDelayedRetry(IRecoverabilityContext recoverabilityContext)
    {
        if (!totalDelayedRetries.Enabled)
        {
            return;
        }

        var incomingPipelineMetricTags = recoverabilityContext.Extensions.Get<IncomingPipelineMetricTags>();
        TagList meterTags;
        incomingPipelineMetricTags.ApplyTags(ref meterTags, [
            MeterTags.QueueName,
            MeterTags.EndpointDiscriminator,
            MeterTags.MessageType,
            MeterTags.MessageHandlerType]);
        // This is what Add(string, object) does so skipping an unnecessary stack frame
        meterTags.Add(new KeyValuePair<string, object>(MeterTags.ErrorType, recoverabilityContext.Exception.GetType().FullName));
        totalDelayedRetries.Add(1, meterTags);
    }

    public void RecordSendToErrorQueue(IRecoverabilityContext recoverabilityContext)
    {
        if (!totalSentToErrorQueue.Enabled)
        {
            return;
        }

        var incomingPipelineMetricTags = recoverabilityContext.Extensions.Get<IncomingPipelineMetricTags>();
        TagList meterTags;
        incomingPipelineMetricTags.ApplyTags(ref meterTags, [
            MeterTags.QueueName,
            MeterTags.EndpointDiscriminator,
            MeterTags.MessageType,
            MeterTags.MessageHandlerType]);
        // This is what Add(string, object) does so skipping an unnecessary stack frame
        meterTags.Add(new KeyValuePair<string, object>(MeterTags.ErrorType, recoverabilityContext.Exception.GetType().FullName));
        totalSentToErrorQueue.Add(1, meterTags);
    }

    readonly Counter<long> totalProcessedSuccessfully;
    readonly Counter<long> totalFetched;
    readonly Counter<long> totalFailures;
    readonly Histogram<double> messageHandlerTime;
    readonly Histogram<double> criticalTime;
    readonly Counter<long> totalImmediateRetries;
    readonly Counter<long> totalDelayedRetries;
    readonly Counter<long> totalSentToErrorQueue;
    string queueNameBase;
    string endpointDiscriminator;
}