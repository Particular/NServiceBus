#nullable enable

namespace NServiceBus;

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Metrics;
using Transport;
using Pipeline;

partial class PipelineMetrics
{
    public const string MeterName = "NServiceBus.Core";
    public const string MeterVersion = "1.0.0";

    const string TotalProcessedSuccessfully = "nservicebus.messaging.successes";
    const string TotalFetched = "nservicebus.messaging.fetches";
    const string TotalFailures = "nservicebus.messaging.failures";
    const string MessageHandlerTime = "nservicebus.messaging.handler_time";
    const string CriticalTime = "nservicebus.messaging.critical_time";
    const string ProcessingTime = "nservicebus.messaging.processing_time";
    const string RecoverabilityImmediate = "nservicebus.recoverability.immediate";
    const string RecoverabilityDelayed = "nservicebus.recoverability.delayed";
    const string RecoverabilityError = "nservicebus.recoverability.error";
    const string EnvelopeUnwrapping = "nservicebus.envelope.unwrapped";
    const string ActiveMessages = "nservicebus.messaging.active_messages";
    const string TotalDeduplicated = "nservicebus.outbox.duplicates";
    const string SagaFetchTime = "nservicebus.sagas.fetch_time";
    const string MessageDeserializeTime = "nservicebus.messaging.deserialize_time";
    const string MessageSerializeTime = "nservicebus.messaging.serialize_time";
    const string OutboxFetchTime = "nservicebus.outbox.fetch_time";
    const string OutboxStoreTime = "nservicebus.outbox.store_time";
    const string CommitTime = "nservicebus.persistence.commit_time";

    // queueName and discriminator are null for send-only endpoints, which have no receive queue to report.
    public PipelineMetrics(IMeterFactory meterFactory, string? queueName, string? discriminator)
    {
        if (V11BehaviorSwitch.UseV11Behavior) // removed in v11, keep only the v11 branch, see obsoletes-v10.cs
        {
            CreateInstruments(meterFactory.Create(MeterName, MeterVersion));
        }
        else
        {
            CreateLegacyInstruments(meterFactory.Create(LegacyMeter.Name, LegacyMeter.Version));
        }

        // queueName and discriminator can be null (e.g., for a send-only endpoint)
        // in such cases we don't want to report these tags
        if (queueName != null)
        {
            DefaultMetricTags.Add(MeterTags.QueueName, queueName);
        }

        if (discriminator != null)
        {
            DefaultMetricTags.Add(MeterTags.EndpointDiscriminator, discriminator);
        }
    }

    [MemberNotNull(
        nameof(totalProcessedSuccessfully), nameof(totalFetched), nameof(totalFailures), nameof(totalDeduplicated),
        nameof(messageHandlerTime), nameof(criticalTime), nameof(processingTime), nameof(totalImmediateRetries),
        nameof(totalDelayedRetries), nameof(totalSentToErrorQueue), nameof(totalEnvelopeUnwrapping), nameof(activeMessages),
        nameof(sagaFetchTime), nameof(messageDeserializeTime), nameof(messageSerializeTime), nameof(outboxFetchTime),
        nameof(outboxStoreTime), nameof(persistenceTime))]
    void CreateInstruments(Meter meter)
    {
        totalProcessedSuccessfully = meter.CreateCounter<long>(TotalProcessedSuccessfully,
            description: "Total number of messages processed successfully by the endpoint.");
        totalFetched = meter.CreateCounter<long>(TotalFetched,
            description: "Total number of messages fetched from the queue by the endpoint.");
        totalFailures = meter.CreateCounter<long>(TotalFailures,
            description: "Total number of messages processed unsuccessfully by the endpoint.");
        totalDeduplicated = meter.CreateCounter<long>(TotalDeduplicated,
            description: "Total number of duplicate messages detected by the Outbox.");
        messageHandlerTime = meter.CreateHistogram(MessageHandlerTime, "s",
            "The time in seconds for the execution of the business code.",
            advice: LongDurationAdvice);
        criticalTime = meter.CreateHistogram(CriticalTime, "s",
            "The time in seconds between when the message was sent until processed by the endpoint.",
            advice: LongDurationAdvice);
        processingTime = meter.CreateHistogram(ProcessingTime, "s",
            "The time in seconds between when the message was fetched from the input queue until successfully processed by the endpoint.",
            advice: LongDurationAdvice);
        totalImmediateRetries = meter.CreateCounter<long>(RecoverabilityImmediate,
            description: "Total number of immediate retries requested.");
        totalDelayedRetries = meter.CreateCounter<long>(RecoverabilityDelayed,
            description: "Total number of delayed retries requested.");
        totalSentToErrorQueue = meter.CreateCounter<long>(RecoverabilityError,
            description: "Total number of messages sent to the error queue.");
        totalEnvelopeUnwrapping = meter.CreateCounter<long>(EnvelopeUnwrapping,
            description: "Total number of unwrapping attempts by the endpoint.");
        activeMessages = meter.CreateUpDownCounter<long>(ActiveMessages,
            description: "Number of messages currently being processed by the endpoint.");
        sagaFetchTime = meter.CreateHistogram(SagaFetchTime, "s",
            "The time in seconds for loading saga data from the persister.",
            advice: ShortDurationAdvice);
        messageDeserializeTime = meter.CreateHistogram(MessageDeserializeTime, "s",
            "The time in seconds for deserializing an incoming message.",
            advice: ShortDurationAdvice);
        messageSerializeTime = meter.CreateHistogram(MessageSerializeTime, "s",
            "The time in seconds for serializing an outgoing message.",
            advice: ShortDurationAdvice);
        outboxFetchTime = meter.CreateHistogram(OutboxFetchTime, "s",
            "The time in seconds for querying the outbox storage for deduplication.",
            advice: ShortDurationAdvice);
        outboxStoreTime = meter.CreateHistogram(OutboxStoreTime, "s",
            "The time in seconds for storing a message in the outbox storage.",
            advice: ShortDurationAdvice);
        persistenceTime = meter.CreateHistogram(CommitTime, "s",
            "The time in seconds for completing the synchronized storage session.",
            advice: ShortDurationAdvice);
    }

    // Values based on: https://opentelemetry.io/docs/specs/semconv/http/http-metrics/#metric-httpserverrequestduration
    static readonly InstrumentAdvice<double> ShortDurationAdvice = new()
    {
        HistogramBucketBoundaries = [0.005, 0.01, 0.025, 0.05, 0.075, 0.1, 0.25, 0.5, 0.75, 1, 2.5, 5, 7.5, 10]
    };

    // Values based on: https://opentelemetry.io/docs/specs/semconv/dotnet/dotnet-kestrel-metrics/#metric-kestrelconnectionduration
    static readonly InstrumentAdvice<double> LongDurationAdvice = new()
    {
        HistogramBucketBoundaries = [0.01, 0.02, 0.05, 0.1, 0.2, 0.5, 1, 2, 5, 10, 30, 60, 120, 300]
    };

    public void RecordProcessingTime(ITransportReceiveContext context, TimeSpan elapsed)
    {
        if (!processingTime.Enabled)
        {
            return;
        }

        var tags = DefaultMetricTags;

        LegacyExecutionResultTag.Add(ref tags); // removed in v11, see obsoletes-v10.cs

        context.PipelineMetricTags.ApplyTags(ref tags, [
            MeterTags.QueueName,
            MeterTags.EndpointDiscriminator,
            MeterTags.MessageType,
            MeterTags.MessageHandlerTypes],
            processingTime.Name);

        processingTime.Record(elapsed.TotalSeconds, tags);
    }

    public void RecordCriticalTimeAndTotalProcessed(ITransportReceiveContext context)
    {
        if (!totalProcessedSuccessfully.Enabled && !criticalTime.Enabled)
        {
            return;
        }

        var tags = DefaultMetricTags;
        LegacyExecutionResultTag.Add(ref tags); // removed in v11, see obsoletes-v10.cs

        // totalProcessedSuccessfully and criticalTime always share the same tags in this method, so overrides are
        // looked up under criticalTime's instrument name.
        context.PipelineMetricTags.ApplyTags(ref tags, [
            MeterTags.QueueName,
            MeterTags.EndpointDiscriminator,
            MeterTags.MessageType,
            MeterTags.MessageHandlerTypes],
            criticalTime.Name);

        if (totalProcessedSuccessfully.Enabled)
        {
            totalProcessedSuccessfully.Add(1, tags);
        }
        if (criticalTime.Enabled)
        {
            if (context.Message.Headers.TryGetDeliverAt(out var startTime)
                || context.Message.Headers.TryGetTimeSent(out startTime))
            {
                var completedAt = DateTimeOffset.UtcNow;
                var criticalTimeElapsed = completedAt - startTime;

                criticalTime.Record(criticalTimeElapsed.TotalSeconds, tags);
            }
        }
    }

    public void RecordMessageProcessingFailure(PipelineMetricTags pipelineMetricTags, Exception error)
    {
        if (!totalFailures.Enabled)
        {
            return;
        }

        var tags = DefaultMetricTags;
        LegacyExecutionResultTag.Add(ref tags, error); // removed in v11, see obsoletes-v10.cs

        tags.Add(MeterTags.ErrorType, error.GetType().FullName);

        pipelineMetricTags.ApplyTags(ref tags, [
            MeterTags.QueueName,
            MeterTags.EndpointDiscriminator,
            MeterTags.MessageType,
            MeterTags.MessageHandlerTypes,
            MeterTags.ErrorType],
            totalFailures.Name);
        totalFailures.Add(1, tags);

        // the processing and critical time are intentionally not recorded in case of failure
    }

    public void RecordFetchedMessage(PipelineMetricTags pipelineMetricTags)
    {
        if (!totalFetched.Enabled)
        {
            return;
        }

        var tags = DefaultMetricTags;
        pipelineMetricTags.ApplyTags(ref tags, [
            MeterTags.EndpointDiscriminator,
            MeterTags.QueueName,
            MeterTags.MessageType],
            totalFetched.Name);

        totalFetched.Add(1, tags);
    }

    public void RecordDeduplicatedMessage(ITransportReceiveContext context)
    {
        if (!totalDeduplicated.Enabled)
        {
            return;
        }

        var tags = DefaultMetricTags;
        context.PipelineMetricTags.ApplyTags(ref tags, [
            MeterTags.EndpointDiscriminator,
            MeterTags.QueueName,
            MeterTags.MessageType],
            totalDeduplicated.Name);

        totalDeduplicated.Add(1, tags);
    }

    public void RecordSuccessfulMessageHandlerTime(IInvokeHandlerContext context, TimeSpan elapsed)
    {
        if (!messageHandlerTime.Enabled)
        {
            return;
        }

        var tags = DefaultMetricTags;
        LegacyExecutionResultTag.Add(ref tags); // removed in v11, see obsoletes-v10.cs

        tags.Add(MeterTags.MessageHandlerType, context.MessageHandler.HandlerType.FullName);

        context.PipelineMetricTags.ApplyTags(ref tags, [
            MeterTags.QueueName,
            MeterTags.EndpointDiscriminator,
            MeterTags.MessageType,
            MeterTags.MessageHandlerType],
            messageHandlerTime.Name);
        messageHandlerTime.Record(elapsed.TotalSeconds, tags);
    }

    public void RecordFailedMessageHandlerTime(IInvokeHandlerContext context, TimeSpan elapsed, Exception error)
    {
        if (!messageHandlerTime.Enabled)
        {
            return;
        }

        var tags = DefaultMetricTags;
        LegacyExecutionResultTag.Add(ref tags, error); // removed in v11, see obsoletes-v10.cs

        tags.Add(MeterTags.MessageHandlerType, context.MessageHandler.HandlerType.FullName);
        tags.Add(MeterTags.ErrorType, error.GetType().FullName);

        context.PipelineMetricTags.ApplyTags(ref tags, [
            MeterTags.QueueName,
            MeterTags.EndpointDiscriminator,
            MeterTags.MessageType,
            MeterTags.MessageHandlerType,
            MeterTags.ErrorType],
            messageHandlerTime.Name);
        messageHandlerTime.Record(elapsed.TotalSeconds, tags);
    }

    public void RecordImmediateRetry(IRecoverabilityContext context)
    {
        if (!totalImmediateRetries.Enabled)
        {
            return;
        }

        var tags = DefaultMetricTags;
        tags.Add(MeterTags.ErrorType, context.Exception.GetType().FullName);

        context.PipelineMetricTags.ApplyTags(ref tags, [
            MeterTags.QueueName,
            MeterTags.EndpointDiscriminator,
            MeterTags.MessageType,
            MeterTags.MessageHandlerType,
            MeterTags.ErrorType],
            totalImmediateRetries.Name
            );
        totalImmediateRetries.Add(1, tags);
    }

    public void RecordDelayedRetry(IRecoverabilityContext context)
    {
        if (!totalDelayedRetries.Enabled)
        {
            return;
        }

        var tags = DefaultMetricTags;
        tags.Add(MeterTags.ErrorType, context.Exception.GetType().FullName);

        context.PipelineMetricTags.ApplyTags(ref tags, [
            MeterTags.QueueName,
            MeterTags.EndpointDiscriminator,
            MeterTags.MessageType,
            MeterTags.MessageHandlerType,
            MeterTags.ErrorType],
            totalDelayedRetries.Name);
        totalDelayedRetries.Add(1, tags);
    }

    public void RecordSendToErrorQueue(IRecoverabilityContext context)
    {
        if (!totalSentToErrorQueue.Enabled)
        {
            return;
        }

        var tags = DefaultMetricTags;
        tags.Add(MeterTags.ErrorType, context.Exception.GetType().FullName);

        context.PipelineMetricTags.ApplyTags(ref tags, [
            MeterTags.QueueName,
            MeterTags.EndpointDiscriminator,
            MeterTags.MessageType,
            MeterTags.MessageHandlerType,
            MeterTags.ErrorType],
            totalSentToErrorQueue.Name);
        totalSentToErrorQueue.Add(1, tags);
    }

    public ActiveMessageScope TrackMessageProcessing(PipelineMetricTags pipelineMetricTags, IncomingMessage message)
    {
        if (!activeMessages.Enabled)
        {
            return default;
        }

        var tags = DefaultMetricTags;
        if (message.Headers.TryGetValue(Headers.EnclosedMessageTypes, out var enclosedMessageTypes))
        {
            tags.Add(MeterTags.EnclosedMessageTypes, enclosedMessageTypes);
        }
        pipelineMetricTags.ApplyTags(ref tags, [
            MeterTags.QueueName,
            MeterTags.EndpointDiscriminator,
            MeterTags.EnclosedMessageTypes],
            activeMessages.Name);

        activeMessages.Add(1, tags);
        return new ActiveMessageScope(activeMessages, tags);
    }

    public void RecordSagaFetchTime(IInvokeHandlerContext context, TimeSpan elapsed, string sagaType, Exception? error = null)
    {
        if (!sagaFetchTime.Enabled)
        {
            return;
        }

        var tags = DefaultMetricTags;
        LegacyExecutionResultTag.Add(ref tags, error); // removed in v11, see obsoletes-v10.cs
        tags.Add(MeterTags.SagaType, sagaType);
        if (error != null)
        {
            tags.Add(MeterTags.ErrorType, error.GetType().FullName);
        }

        context.PipelineMetricTags.ApplyTags(ref tags, [
            MeterTags.QueueName,
            MeterTags.EndpointDiscriminator,
            MeterTags.MessageType,
            MeterTags.SagaType,
            MeterTags.ErrorType],
            sagaFetchTime.Name);
        sagaFetchTime.Record(elapsed.TotalSeconds, tags);
    }

    public void RecordDeserializeTime(IIncomingPhysicalMessageContext context, TimeSpan elapsed, Exception? error = null)
    {
        if (!messageDeserializeTime.Enabled)
        {
            return;
        }

        var tags = DefaultMetricTags;
        if (context.Message.Headers.TryGetValue(Headers.EnclosedMessageTypes, out var messageTypes))
        {
            tags.Add(MeterTags.EnclosedMessageTypes, messageTypes);
        }
        if (error != null)
        {
            tags.Add(MeterTags.ErrorType, error.GetType().FullName);
        }
        LegacyExecutionResultTag.Add(ref tags, error); // removed in v11, see obsoletes-v10.cs

        context.PipelineMetricTags.ApplyTags(ref tags, [
            MeterTags.QueueName,
            MeterTags.EndpointDiscriminator,
            MeterTags.EnclosedMessageTypes,
            MeterTags.ErrorType],
            messageDeserializeTime.Name);

        messageDeserializeTime.Record(elapsed.TotalSeconds, tags);
    }

    public void RecordSerializeTime(IOutgoingLogicalMessageContext context, TimeSpan elapsed, string? messageType, Exception? error = null)
    {
        if (!messageSerializeTime.Enabled)
        {
            return;
        }

        var tags = DefaultMetricTags;
        if (messageType != null)
        {
            tags.Add(MeterTags.MessageType, messageType);
        }
        if (error != null)
        {
            tags.Add(MeterTags.ErrorType, error.GetType().FullName);
        }
        LegacyExecutionResultTag.Add(ref tags, error); // removed in v11, see obsoletes-v10.cs

        context.PipelineMetricTags.ApplyTags(ref tags, [
                MeterTags.QueueName,
                MeterTags.EndpointDiscriminator,
                MeterTags.MessageType,
                MeterTags.ErrorType],
            messageSerializeTime.Name);

        messageSerializeTime.Record(elapsed.TotalSeconds, tags);
    }

    public void RecordOutboxFetchTime(ITransportReceiveContext context, TimeSpan elapsed)
    {
        if (!outboxFetchTime.Enabled)
        {
            return;
        }

        var tags = DefaultMetricTags;
        context.PipelineMetricTags.ApplyTags(ref tags, [
            MeterTags.QueueName,
            MeterTags.EndpointDiscriminator],
            outboxFetchTime.Name);

        outboxFetchTime.Record(elapsed.TotalSeconds, tags);
    }

    public void RecordOutboxStoreTime(ITransportReceiveContext context, TimeSpan elapsed)
    {
        if (!outboxStoreTime.Enabled)
        {
            return;
        }

        var tags = DefaultMetricTags;
        context.PipelineMetricTags.ApplyTags(ref tags, [
            MeterTags.QueueName,
            MeterTags.EndpointDiscriminator],
            outboxStoreTime.Name);

        outboxStoreTime.Record(elapsed.TotalSeconds, tags);
    }

    public void RecordPersistenceTime(IIncomingLogicalMessageContext context, TimeSpan elapsed)
    {
        if (!persistenceTime.Enabled)
        {
            return;
        }

        var tags = DefaultMetricTags;
        context.PipelineMetricTags.ApplyTags(ref tags, [
            MeterTags.QueueName,
            MeterTags.EndpointDiscriminator,
            MeterTags.MessageType,
            MeterTags.MessageHandlerTypes],
            persistenceTime.Name);

        persistenceTime.Record(elapsed.TotalSeconds, tags);
    }

    public void EnvelopeUnwrappingSucceeded(MessageContext messageContext, IEnvelopeHandler type) => RecordEnvelopeUnwrapping(messageContext, type, true, null);
    public void EnvelopeUnwrappingFailed(MessageContext messageContext, IEnvelopeHandler type, Exception? exception) => RecordEnvelopeUnwrapping(messageContext, type, false, exception);
    void RecordEnvelopeUnwrapping(MessageContext messageContext, IEnvelopeHandler type, bool succeeded, Exception? exception)
    {
        if (!totalEnvelopeUnwrapping.Enabled)
        {
            return;
        }

        var tags = DefaultMetricTags;
        tags.Add(MeterTags.EnvelopeUnwrapperType, type.GetType().FullName);

        if (exception != null)
        {
            tags.Add(MeterTags.ErrorType, exception.GetType().FullName);
        }

        messageContext.MetricTags.ApplyTags(ref tags, [
            MeterTags.QueueName,
            MeterTags.EndpointDiscriminator,
            MeterTags.EnvelopeUnwrapperType,
            MeterTags.ErrorType],
            totalEnvelopeUnwrapping.Name);

        totalEnvelopeUnwrapping.Add(succeeded ? 0 : 1, tags);
    }

    public readonly struct ActiveMessageScope(UpDownCounter<long>? counter, TagList tags) : IDisposable
    {
        public void Dispose() => counter?.Add(-1, tags);
    }

    Counter<long> totalProcessedSuccessfully;
    Counter<long> totalFetched;
    Counter<long> totalFailures;
    Counter<long> totalDeduplicated;
    Histogram<double> messageHandlerTime;
    Histogram<double> criticalTime;
    Histogram<double> processingTime;
    Counter<long> totalImmediateRetries;
    Counter<long> totalDelayedRetries;
    Counter<long> totalSentToErrorQueue;
    Counter<long> totalEnvelopeUnwrapping;
    UpDownCounter<long> activeMessages;
    Histogram<double> sagaFetchTime;
    Histogram<double> messageDeserializeTime;
    Histogram<double> messageSerializeTime;
    Histogram<double> outboxFetchTime;
    Histogram<double> outboxStoreTime;
    Histogram<double> persistenceTime;

    readonly TagList DefaultMetricTags;
}
