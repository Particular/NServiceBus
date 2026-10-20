#nullable enable

namespace NServiceBus;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Extensibility;
using Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using DelayedDelivery;
using Outbox;
using Persistence;
using Routing;
using Sagas;
using Transport;
using Unicast.Messages;
using TransportOperation = Transport.TransportOperation;

// Applies timeout changes requested by an operator in ServiceControl. Satellites bypass the message pipeline,
// so the saga is loaded and saved directly, and the outbox is used the same way TransportReceiveToPhysicalMessageConnector uses it.
sealed partial class SagaTimeoutManagementSatellite(
    ISagaPersister persister,
    SagaMetadataCollection sagaMetadataCollection,
    MessageMetadataRegistry messageMetadataRegistry,
    TimeoutSerializer timeoutSerializer,
    IMessageDispatcher dispatcher,
    IOutboxStorage outboxStorage,
    SagaTimeoutPlatformNotifier platformNotifier,
    string endpointName,
    HostInformation hostInformation,
    string mainReceiveAddress,
    ILogger<SagaTimeoutManagementSatellite> logger)
{
    public async Task Handle(IServiceProvider serviceProvider, MessageContext messageContext, CancellationToken cancellationToken = default)
    {
        var request = SagaTimeoutManagementRequest.Parse(messageContext.Headers, messageContext.Body);
        var incomingMessage = new IncomingMessage(
            messageContext.NativeMessageId,
            messageContext.Headers,
            messageContext.Body,
            messageContext.ReceiveProperties);

        var messageId = incomingMessage.MessageId;
        var contextBag = new ContextBag(messageContext.Extensions);
        contextBag.Set(incomingMessage);

        var deduplicationEntry = await outboxStorage.Get(messageId, contextBag, cancellationToken).ConfigureAwait(false);
        TransportOperation[] operations;

        if (deduplicationEntry is null)
        {
            var outboxTransaction = await outboxStorage.BeginTransaction(contextBag, cancellationToken).ConfigureAwait(false);
            await using (outboxTransaction.ConfigureAwait(false))
            {
                operations = await Process(serviceProvider, request, messageContext, outboxTransaction, contextBag, cancellationToken).ConfigureAwait(false);

                var outboxMessage = new OutboxMessage(messageId, TransportReceiveToPhysicalMessageConnector.ConvertToOutboxOperations(operations));
                await outboxStorage.Store(outboxMessage, outboxTransaction, contextBag, cancellationToken).ConfigureAwait(false);
                await outboxTransaction.Commit(cancellationToken).ConfigureAwait(false);
            }
        }
        else
        {
            LogOutboxDuplicate(logger, messageId);
            var pendingOperations = new PendingTransportOperations();
            TransportReceiveToPhysicalMessageConnector.ConvertToPendingOperations(deduplicationEntry, pendingOperations);
            operations = pendingOperations.Operations;
        }

        if (operations.Length > 0)
        {
            await dispatcher.Dispatch(new TransportOperations(operations), messageContext.TransportTransaction, cancellationToken).ConfigureAwait(false);
        }

        await outboxStorage.SetAsDispatched(messageId, contextBag, cancellationToken).ConfigureAwait(false);
    }

    async Task<TransportOperation[]> Process(IServiceProvider serviceProvider, SagaTimeoutManagementRequest request, MessageContext messageContext, IOutboxTransaction outboxTransaction, ContextBag contextBag, CancellationToken cancellationToken)
    {
        var operations = new List<TransportOperation>();

        var scope = serviceProvider.CreateAsyncScope();
        await using (scope.ConfigureAwait(false))
        {
            var storageSession = scope.ServiceProvider.GetService<ICompletableSynchronizedStorageSession>()
                                 ?? NoOpCompletableSynchronizedStorageSession.Instance;

            await using (storageSession.ConfigureAwait(false))
            {
                await storageSession.Open(outboxTransaction, messageContext.TransportTransaction, contextBag, cancellationToken).ConfigureAwait(false);
                await Apply(request, messageContext.Body, storageSession, contextBag, operations, cancellationToken).ConfigureAwait(false);
                // A rejected request saves nothing, but the session still completes so the rejection report is stored with the outbox transaction
                await storageSession.CompleteAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        return [.. operations];
    }

    async Task Apply(SagaTimeoutManagementRequest request, ReadOnlyMemory<byte> body, ICompletableSynchronizedStorageSession storageSession, ContextBag contextBag, List<TransportOperation> operations, CancellationToken cancellationToken)
    {
        var metadata = FindSagaMetadata(request.SagaType);
        var sagaData = metadata is null
            ? null
            : await metadata.Loader.Load(persister, request.SagaId.ToString(), storageSession, contextBag, cancellationToken).ConfigureAwait(false);

        if (metadata is null || sagaData is null)
        {
            LogSagaNotFound(logger, request.OperationId, request.SagaId, request.SagaType);
            await platformNotifier.NotifyRejected(request.SagaType, request.SagaId, request.TimeoutId, request.Change, request.DueTime, SagaTimeoutChangeRejectionReasons.SagaNotFound, request.OperationId, operations, cancellationToken).ConfigureAwait(false);
            return;
        }

        var timeouts = new SagaTimeouts(contextBag.GetOrCreateSagaTimeouts(sagaData), timeoutSerializer, messageMetadataRegistry);
        var stored = timeouts.Get(request.TimeoutId);
        var rejectedKind = request.IsReschedule ? SagaTimeoutChangeKind.RescheduleRejected : SagaTimeoutChangeKind.CancelRejected;

        if (stored is null || stored.Version != request.ExpectedVersion)
        {
            var reason = stored is null ? SagaTimeoutChangeRejectionReasons.TimeoutNotFound : SagaTimeoutChangeRejectionReasons.VersionMismatch;
            LogChangeRejected(logger, request.OperationId, request.TimeoutId, reason);
            timeouts.Reject(rejectedKind, request.TimeoutId, request.DueTime, reason, request.OperationId);
            await platformNotifier.NotifyChanges(metadata, sagaData, timeouts.Changes, operations, cancellationToken).ConfigureAwait(false);
            return;
        }

        if (request.IsReschedule)
        {
            var rescheduled = timeouts.RescheduleRequestedByOperator(stored, request.DueTime!.Value, body, request.PayloadContentType, request.OperationId);
            operations.Add(CreateTimeoutMessage(metadata, sagaData, rescheduled));
        }
        else
        {
            timeouts.Cancel(request.TimeoutId, request.OperationId);
        }

        await persister.Update(sagaData, storageSession, contextBag, cancellationToken).ConfigureAwait(false);
        await platformNotifier.NotifyChanges(metadata, sagaData, timeouts.Changes, operations, cancellationToken).ConfigureAwait(false);
    }

    SagaMetadata? FindSagaMetadata(string sagaType)
    {
        foreach (var metadata in sagaMetadataCollection)
        {
            if (metadata.SagaType.FullName == sagaType)
            {
                return metadata;
            }
        }

        return null;
    }

    // Mirrors the headers the saga pipeline sets on a timeout, without serializing the stored payload again
    TransportOperation CreateTimeoutMessage(SagaMetadata metadata, IContainSagaData sagaData, StoredSagaTimeout timeout)
    {
        var messageId = CombGuid.Generate().ToString();
        var now = DateTimeOffset.UtcNow;
        var headers = new Dictionary<string, string>
        {
            [Headers.MessageId] = messageId,
            [Headers.CorrelationId] = messageId,
            [Headers.ConversationId] = CombGuid.Generate().ToString(),
            [Headers.MessageIntent] = MessageIntent.Send.ToString(),
            [Headers.EnclosedMessageTypes] = messageMetadataRegistry.GetMessageMetadata(timeout.MessageType)?.MessageHierarchySerialized ?? timeout.MessageType,
            [Headers.ContentType] = timeout.ContentType,
            [Headers.ReplyToAddress] = mainReceiveAddress,
            [Headers.OriginatingEndpoint] = endpointName,
            [Headers.OriginatingMachine] = hostInformation.DisplayName,
            [Headers.OriginatingHostId] = hostInformation.HostId.ToString("N"),
            [Headers.NServiceBusVersion] = VersionInformation.MajorMinorPatch,
            [Headers.TimeSent] = DateTimeOffsetHelper.ToWireFormattedString(now),
            [Headers.SagaId] = sagaData.Id.ToString(),
            [Headers.SagaType] = metadata.SagaType.AssemblyQualifiedName!,
            [Headers.IsSagaTimeoutMessage] = bool.TrueString,
            [SagaTimeouts.TimeoutIdHeader] = SagaTimeouts.Identity(timeout)
        };

        var properties = new DispatchProperties();
        if (timeout.DueTime > now)
        {
            properties.DoNotDeliverBefore = new DoNotDeliverBefore(timeout.DueTime);
        }

        return new TransportOperation(new OutgoingMessage(messageId, headers, timeout.Payload), new UnicastAddressTag(mainReceiveAddress), properties);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Rejecting saga timeout change {OperationId} because saga {SagaId} of type {SagaType} wasn't found.")]
    static partial void LogSagaNotFound(ILogger logger, Guid operationId, Guid sagaId, string sagaType);

    [LoggerMessage(Level = LogLevel.Information, Message = "Rejecting saga timeout change {OperationId} for timeout {TimeoutId}: {Reason}.")]
    static partial void LogChangeRejected(ILogger logger, Guid operationId, string timeoutId, string reason);

    [LoggerMessage(Level = LogLevel.Information, Message = "Outbox duplicate detected for saga timeout management request '{MessageId}'. Dispatching the stored operations without applying the request again.")]
    static partial void LogOutboxDuplicate(ILogger logger, string messageId);
}
