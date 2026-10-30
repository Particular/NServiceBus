#nullable enable

namespace NServiceBus;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DelayedDelivery;
using Hosting;
using Microsoft.Extensions.Logging;
using Routing;
using Sagas;
using Transport;
using Unicast.Messages;

// Applies timeout changes requested by an operator in ServiceControl. The request arrives on the control queue, whose
// receiver runs the main receive pipeline, so the outbox, the synchronized storage session, and the persistence's
// behaviors apply as they do to any message. The saga isn't invoked, because this isn't a saga handler.
sealed partial class SagaTimeoutChangeRequestHandler(
    ISagaPersister persister,
    ITimeoutPersister timeoutPersister,
    MessageMetadataRegistry messageMetadataRegistry,
    TimeoutSerializer timeoutSerializer,
    SagaTimeoutPlatformNotifier platformNotifier,
    SagaTimeoutChangeRequestHandler.EndpointInfo endpointInfo,
    HostInformation hostInformation,
    ILogger<SagaTimeoutChangeRequestHandler> logger) : IHandleMessages<SagaTimeoutChangeRequest>
{
    public async Task Handle(SagaTimeoutChangeRequest request, IMessageHandlerContext context)
    {
        var storageSession = context.SynchronizedStorageSession;
        var contextBag = context.Extensions;

        var metadata = FindSagaMetadata(request.SagaType);
        var sagaData = metadata is null
            ? null
            : await metadata.Loader.Load(persister, request.SagaId.ToString(), storageSession, contextBag, context.CancellationToken).ConfigureAwait(false);

        if (metadata is null || sagaData is null)
        {
            LogSagaNotFound(logger, request.OperationId, request.SagaId, request.SagaType);
            await platformNotifier.NotifyRejected(request.SagaType, request.SagaId, request.TimeoutId, request.Change, request.DueTime, SagaTimeoutChangeRejectionReasons.SagaNotFound, request.OperationId, context).ConfigureAwait(false);
            return;
        }

        var storedTimeouts = await timeoutPersister.Get(sagaData, storageSession, contextBag, context.CancellationToken).ConfigureAwait(false);
        var timeouts = new SagaTimeouts(storedTimeouts, timeoutSerializer, messageMetadataRegistry);
        var stored = timeouts.Get(request.TimeoutId);
        var rejectedKind = request.IsReschedule ? SagaTimeoutChangeKind.RescheduleRejected : SagaTimeoutChangeKind.CancelRejected;

        if (stored is null || stored.Version != request.ExpectedVersion)
        {
            var reason = stored is null ? SagaTimeoutChangeRejectionReasons.TimeoutNotFound : SagaTimeoutChangeRejectionReasons.VersionMismatch;
            LogChangeRejected(logger, request.OperationId, request.TimeoutId, reason);
            timeouts.Reject(rejectedKind, request.TimeoutId, request.DueTime, reason, request.OperationId);
            await platformNotifier.NotifyChanges(metadata, sagaData, timeouts.Changes, context).ConfigureAwait(false);
            return;
        }

        if (request.IsReschedule)
        {
            var rescheduled = timeouts.RescheduleRequestedByOperator(stored, request.DueTime!.Value, request.Payload ?? ReadOnlyMemory<byte>.Empty, request.PayloadContentType, request.OperationId);
            // Joins the outgoing operations of the request, so the outbox stores it with the platform reports
            context.Extensions.Get<PendingTransportOperations>().Add(CreateTimeoutMessage(metadata, sagaData, rescheduled));
        }
        else
        {
            timeouts.Cancel(request.TimeoutId, request.OperationId);
        }

        // Updating the unchanged saga data makes the saga's optimistic concurrency check conflict with a handler that changes the same timeouts
        await persister.Update(sagaData, storageSession, contextBag, context.CancellationToken).ConfigureAwait(false);
        await timeouts.Persist(timeoutPersister, sagaData, storageSession, contextBag, context.CancellationToken).ConfigureAwait(false);
        await platformNotifier.NotifyChanges(metadata, sagaData, timeouts.Changes, context).ConfigureAwait(false);
    }

    SagaMetadata? FindSagaMetadata(string sagaType)
    {
        foreach (var metadata in endpointInfo.Sagas)
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
            [Headers.ReplyToAddress] = endpointInfo.MainReceiveAddress,
            [Headers.OriginatingEndpoint] = endpointInfo.EndpointName,
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

        return new TransportOperation(new OutgoingMessage(messageId, headers, timeout.Payload), new UnicastAddressTag(endpointInfo.MainReceiveAddress), properties);
    }

    // Registered once by the feature, so the handler doesn't need these as separate container services
    public sealed record EndpointInfo(string EndpointName, string MainReceiveAddress, SagaMetadataCollection Sagas);

    [LoggerMessage(Level = LogLevel.Information, Message = "Rejecting saga timeout change {OperationId} because saga {SagaId} of type {SagaType} wasn't found.")]
    static partial void LogSagaNotFound(ILogger logger, Guid operationId, Guid sagaId, string sagaType);

    [LoggerMessage(Level = LogLevel.Information, Message = "Rejecting saga timeout change {OperationId} for timeout {TimeoutId}: {Reason}.")]
    static partial void LogChangeRejected(ILogger logger, Guid operationId, string timeoutId, string reason);
}
