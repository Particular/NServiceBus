namespace NServiceBus;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Outbox;
using Pipeline;
using Routing;
using Transport;
using TransportOperation = Outbox.TransportOperation;

class TransportReceiveToPhysicalMessageConnector : IStageForkConnector<ITransportReceiveContext, IIncomingPhysicalMessageContext, IBatchDispatchContext>
{
    public TransportReceiveToPhysicalMessageConnector(IOutboxStorage outboxStorage, IncomingPipelineMetrics incomingPipelineMetrics)
    {
        this.outboxStorage = outboxStorage;
        this.incomingPipelineMetrics = incomingPipelineMetrics;
    }

    public async Task Invoke(ITransportReceiveContext context, Func<IIncomingPhysicalMessageContext, Task> next)
    {
        var processingStartedAt = DateTimeOffset.UtcNow;
        var messageId = context.Message.MessageId;
        var physicalMessageContext = this.CreateIncomingPhysicalMessageContext(context.Message, context);

        var deduplicationEntry = await outboxStorage.Get(messageId, context.Extensions, context.CancellationToken).ConfigureAwait(false);
        var pendingTransportOperations = new PendingTransportOperations();
        if (deduplicationEntry == null)
        {
            physicalMessageContext.Extensions.Set(pendingTransportOperations);

            using (var outboxTransaction = await outboxStorage.BeginTransaction(context.Extensions, context.CancellationToken).ConfigureAwait(false))
            {
                context.Extensions.Set(outboxTransaction);
                await next(physicalMessageContext).ConfigureAwait(false);

                var outboxMessage = new OutboxMessage(messageId, ConvertToOutboxOperations(pendingTransportOperations.Operations));
                await outboxStorage.Store(outboxMessage, outboxTransaction, context.Extensions, context.CancellationToken).ConfigureAwait(false);

                context.Extensions.Remove<IOutboxTransaction>();
                await outboxTransaction.Commit(context.CancellationToken).ConfigureAwait(false);
            }

            // We are measuring outside the transaction scope instead of right after the transaction is committed.
            // Under some specific configurations the heavy lifting is not done as part of the commit but
            // as part of the transaction scope dispose (e.g., when using SQL with transaction scope and DTC)
            var processingCompletedAt = DateTimeOffset.UtcNow;
            incomingPipelineMetrics.RecordProcessingTime(context, processingCompletedAt - processingStartedAt);

            physicalMessageContext.Extensions.Remove<PendingTransportOperations>();
        }
        else
        {
            context.Extensions.TryGetRecordingIncomingPipelineActivity(out var activity);
            activity?.AddTag("nservicebus.outbox.deduplicate-message", true);
            ConvertToPendingOperations(deduplicationEntry, pendingTransportOperations);
        }

        if (pendingTransportOperations.HasOperations)
        {
            var batchDispatchContext = this.CreateBatchDispatchContext(pendingTransportOperations.Operations, physicalMessageContext);

            if (context.Extensions.TryGetRecordingIncomingPipelineActivity(out var activity))
            {
                var activityTagsCollection = new ActivityTagsCollection { { "message-count", batchDispatchContext.Operations.Count } };
                activity?.AddEvent(new ActivityEvent("Start dispatching", tags: activityTagsCollection));
            }

            await this.Fork(batchDispatchContext).ConfigureAwait(false);
            activity?.AddEvent(new ActivityEvent("Finished dispatching"));
        }

        await outboxStorage.SetAsDispatched(messageId, context.Extensions, context.CancellationToken).ConfigureAwait(false);

        if (pendingTransportOperations.HasOperations || deduplicationEntry == null)
        {
            incomingPipelineMetrics.RecordCriticalTimeAndTotalProcessed(context);
        }
    }

    static void ConvertToPendingOperations(OutboxMessage deduplicationEntry, PendingTransportOperations pendingTransportOperations)
    {
        foreach (var operation in deduplicationEntry.TransportOperations)
        {
            var message = new OutgoingMessage(operation.MessageId, operation.Headers, operation.Body);

            pendingTransportOperations.Add(
                new Transport.TransportOperation(
                    message,
                    DeserializeRoutingStrategy(operation.Options),
                    operation.Options,
                    DispatchConsistency.Isolated
                    ));
        }
    }

    static TransportOperation[] ConvertToOutboxOperations(Transport.TransportOperation[] operations)
    {
        var transportOperations = new TransportOperation[operations.Length];
        var index = 0;
        foreach (var operation in operations)
        {
            SerializeRoutingStrategy(operation.AddressTag, operation.Properties);

            transportOperations[index] = new TransportOperation(operation.Message.MessageId, operation.Properties, operation.Message.Body, operation.Message.Headers);
            index++;
        }
        return transportOperations;
    }

    static void SerializeRoutingStrategy(AddressTag addressTag, Dictionary<string, string> options)
    {
        if (addressTag is MulticastAddressTag indirect)
        {
            options["EventType"] = indirect.MessageType.AssemblyQualifiedName;
            return;
        }

        if (addressTag is UnicastAddressTag direct)
        {
            options["Destination"] = direct.Destination;
            return;
        }

        throw new Exception($"Unknown routing strategy {addressTag.GetType().FullName}");
    }

    static AddressTag DeserializeRoutingStrategy(Dictionary<string, string> options)
    {
        if (options.Remove("Destination", out var destination))
        {
            return new UnicastAddressTag(destination);
        }

        if (options.Remove("EventType", out var eventType))
        {
            return new MulticastAddressTag(Type.GetType(eventType, true));
        }

        throw new Exception("Could not find routing strategy to deserialize");
    }

    readonly IOutboxStorage outboxStorage;
    readonly IncomingPipelineMetrics incomingPipelineMetrics;
}
