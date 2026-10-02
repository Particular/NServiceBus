#nullable enable

namespace NServiceBus;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Outbox;
using Pipeline;
using Routing;
using Transport;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;
using TransportOperation = Outbox.TransportOperation;

partial class TransportReceiveToPhysicalMessageConnector(
    IOutboxStorage outboxStorage,
    PipelineMetrics pipelineMetrics,
    ILogger<TransportReceiveToPhysicalMessageConnector> logger)
    : IStageForkConnector<ITransportReceiveContext, IIncomingPhysicalMessageContext, IBatchDispatchContext>
{
    // When no outbox is configured the storage is a no-op that discards whatever it is handed, so building an
    // OutboxMessage for it is pure waste on every message that dispatches anything.
    readonly bool outboxEnabled = outboxStorage is not NoOpOutboxStorage;

    // Invoke is deliberately not async: it has no state machine of its own, so exactly one is boxed per
    // message, sized for the path actually taken.
    public Task Invoke(ITransportReceiveContext context, Func<IIncomingPhysicalMessageContext, Task> next)
        => outboxEnabled ? InvokeWithOutbox(context, next) : InvokeWithoutOutbox(context, next);

    async Task InvokeWithOutbox(ITransportReceiveContext context, Func<IIncomingPhysicalMessageContext, Task> next)
    {
        var processingStartedAt = Stopwatch.GetTimestamp();
        var messageId = context.Message.MessageId;
        var physicalMessageContext = this.CreateIncomingPhysicalMessageContext(context.Message, context);

        var outboxFetchStart = Stopwatch.GetTimestamp();
        var deduplicationEntry = await outboxStorage.Get(messageId, context.Extensions, context.CancellationToken).ConfigureAwait(false);
        pipelineMetrics.RecordOutboxFetchTime(context, Stopwatch.GetElapsedTime(outboxFetchStart));
        var pendingTransportOperations = new PendingTransportOperations();
        // Materialized once: PendingTransportOperations.Operations snapshots a ConcurrentStack on every access.
        Transport.TransportOperation[] operations;
        if (deduplicationEntry == null)
        {
            physicalMessageContext.Extensions.Set(pendingTransportOperations);

            var outboxTransaction = await outboxStorage.BeginTransaction(context.Extensions, context.CancellationToken)
                .ConfigureAwait(false);

            await using (outboxTransaction.ConfigureAwait(false))
            {
                context.Extensions.Set(outboxTransaction);
                await next(physicalMessageContext).ConfigureAwait(false);

                operations = pendingTransportOperations.Operations;

                var outboxMessage = new OutboxMessage(messageId, ConvertToOutboxOperations(operations));
                var outboxStoreStart = Stopwatch.GetTimestamp();
                await outboxStorage.Store(outboxMessage, outboxTransaction, context.Extensions, context.CancellationToken).ConfigureAwait(false);
                pipelineMetrics.RecordOutboxStoreTime(context, Stopwatch.GetElapsedTime(outboxStoreStart));

                context.Extensions.Remove<IOutboxTransaction>();
                await outboxTransaction.Commit(context.CancellationToken).ConfigureAwait(false);
            }

            // We are measuring outside the transaction scope instead of right after the transaction is committed.
            // Under some specific configurations the heavy lifting is not done as part of the commit but
            // as part of the transaction scope dispose (e.g., when using SQL with transaction scope and DTC)
            var elapsedTime = Stopwatch.GetElapsedTime(processingStartedAt);
            pipelineMetrics.RecordProcessingTime(context, elapsedTime);

            physicalMessageContext.Extensions.Remove<PendingTransportOperations>();
        }
        else
        {
            LogOutboxDuplicateDetectedForMessageMessageIdSkippingHandlerExecution(messageId);
            context.Extensions.TryGetIncomingPipelineActivity(out var activity);
            activity?.AddTag("nservicebus.outbox.deduplicate-message", true);
            pipelineMetrics.RecordDeduplicatedMessage(context);
            ConvertToPendingOperations(deduplicationEntry, pendingTransportOperations);
            operations = pendingTransportOperations.Operations;
        }

        if (operations.Length > 0)
        {
            var batchDispatchContext = this.CreateBatchDispatchContext(operations, physicalMessageContext);
            await Dispatch(batchDispatchContext).ConfigureAwait(false);
        }

        await outboxStorage.SetAsDispatched(messageId, context.Extensions, context.CancellationToken).ConfigureAwait(false);

        if (operations.Length > 0 || deduplicationEntry == null)
        {
            pipelineMetrics.RecordCriticalTimeAndTotalProcessed(context);
        }
    }

    async Task InvokeWithoutOutbox(ITransportReceiveContext context, Func<IIncomingPhysicalMessageContext, Task> next)
    {
        // Without an outbox there is nothing to deduplicate against: NoOpOutboxStorage.Get always returns null
        // and SetAsDispatched does nothing, so only the fresh-processing path is reachable and neither call is
        // made. No outbox transaction is parked in the context either; the storage session substitutes the
        // no-op transaction when none is present.
        var processingStartedAt = Stopwatch.GetTimestamp();
        var physicalMessageContext = this.CreateIncomingPhysicalMessageContext(context.Message, context);

        var pendingTransportOperations = new PendingTransportOperations();
        physicalMessageContext.Extensions.Set(pendingTransportOperations);

        await next(physicalMessageContext).ConfigureAwait(false);

        // Materialized once: PendingTransportOperations.Operations snapshots a ConcurrentStack on every access.
        var operations = pendingTransportOperations.Operations;

        var elapsedTime = Stopwatch.GetElapsedTime(processingStartedAt);
        pipelineMetrics.RecordProcessingTime(context, elapsedTime);

        physicalMessageContext.Extensions.Remove<PendingTransportOperations>();

        if (operations.Length > 0)
        {
            var batchDispatchContext = this.CreateBatchDispatchContext(operations, physicalMessageContext);
            await Dispatch(batchDispatchContext).ConfigureAwait(false);
        }

        pipelineMetrics.RecordCriticalTimeAndTotalProcessed(context);
    }

    // Both paths dispatch through here so the event instrumentation lives in one place. The activity is resolved
    // from the batch dispatch context; child context bags read through to their parent, so this sees the same
    // activity as the root receive context. When the events are off (the pre-v11 default) this returns the Fork
    // task directly and adds no state machine of its own.
    Task Dispatch(IBatchDispatchContext batchDispatchContext)
    {
        if (!V11BehaviorSwitch.UseV11Behavior && // removed in v11 together with the events, see obsoletes-v10.cs
            batchDispatchContext.Extensions.TryGetIncomingPipelineActivity(out var activity))
        {
            return DispatchWithEvents(batchDispatchContext, activity);
        }

        return this.Fork(batchDispatchContext);
    }

    async Task DispatchWithEvents(IBatchDispatchContext batchDispatchContext, Activity activity)
    {
        activity.AddEvent(new("Start dispatching", tags: new() { { "message-count", batchDispatchContext.Operations.Count } }));
        await this.Fork(batchDispatchContext).ConfigureAwait(false);
        activity.AddEvent(new("Finished dispatching"));
    }

    static void ConvertToPendingOperations(OutboxMessage deduplicationEntry, PendingTransportOperations pendingTransportOperations)
    {
        foreach (var operation in deduplicationEntry.TransportOperations)
        {
            var message = new OutgoingMessage(operation.MessageId, operation.Headers, operation.Body);

            pendingTransportOperations.Add(new(message, DeserializeRoutingStrategy(operation.Options), operation.Options, DispatchConsistency.Isolated));
        }
    }

    static TransportOperation[] ConvertToOutboxOperations(Transport.TransportOperation[] operations)
    {
        var transportOperations = new TransportOperation[operations.Length];
        for (int index = 0; index < operations.Length; index++)
        {
            var operation = operations[index];
            SerializeRoutingStrategy(operation.AddressTag, operation.Properties);

            transportOperations[index] = new(operation.Message.MessageId, operation.Properties, operation.Message.Body, operation.Message.Headers);
        }

        return transportOperations;
    }

    static void SerializeRoutingStrategy(AddressTag addressTag, DispatchProperties options)
    {
        switch (addressTag)
        {
            case MulticastAddressTag indirect:
                options["EventType"] = indirect.MessageType.AssemblyQualifiedName!;
                return;
            case UnicastAddressTag direct:
                options["Destination"] = direct.Destination;
                return;
            default:
                throw new Exception($"Unknown routing strategy {addressTag.GetType().FullName}");
        }
    }

    static AddressTag DeserializeRoutingStrategy(DispatchProperties? options)
    {
        if (options is not null)
        {
            if (options.Remove("Destination", out var destination))
            {
                return new UnicastAddressTag(destination);
            }

            if (options.Remove("EventType", out var eventType))
            {
                return new MulticastAddressTag(Type.GetType(eventType, true));
            }
        }

        throw new Exception("Could not find routing strategy to deserialize");
    }

    [LoggerMessage(LogLevel.Information, "Outbox duplicate detected for message '{MessageId}'. Skipping handler execution")]
    partial void LogOutboxDuplicateDetectedForMessageMessageIdSkippingHandlerExecution(string messageId);
}
