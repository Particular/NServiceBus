#nullable enable

namespace NServiceBus;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Extensibility;
using Persistence;
using Sagas;
using Unicast.Messages;

class SagaTimeouts(IReadOnlyList<StoredSagaTimeout> loadedTimeouts, TimeoutSerializer serializer, MessageMetadataRegistry messageMetadataRegistry)
{
    public const string TimeoutIdHeader = "NServiceBus.SagaTimeoutId";

    public IReadOnlyCollection<PendingSagaTimeout> PendingTimeouts => pendingTimeouts.Values;

    public IReadOnlyList<SagaTimeoutChange> Changes => changes;

    // Captured before any change, so a handler that removes the last timeout and completes the saga still reports the completion
    public bool HadStoredTimeoutsWhenLoaded { get; } = loadedTimeouts.Count > 0;

    public StoredSagaTimeout Add<TTimeoutMessageType>(TTimeoutMessageType payload, DateTimeOffset dueTime)
    {
        var timeout = new StoredSagaTimeout
        {
            TimeoutId = Guid.NewGuid().ToString(),
            TriggerId = Guid.NewGuid().ToString(),
            Version = 1,
            DueTime = dueTime,
            MessageType = MessageTypeName(typeof(TTimeoutMessageType)),
            ContentType = serializer.ContentType,
            Payload = Serialize(payload!)
        };

        storedTimeouts.Add(timeout);
        pendingTimeouts[timeout.TimeoutId] = new PendingSagaTimeout(timeout, typeof(TTimeoutMessageType));
        operations.Add(new TimeoutOperation(TimeoutOperationKind.Request, timeout.TimeoutId, Snapshot(timeout)));
        changes.Add(new SagaTimeoutChange(SagaTimeoutChangeKind.Requested, timeout.TimeoutId, Snapshot(timeout), null));
        return timeout;
    }

    public IEnumerable<SagaTimeout<TTimeoutMessageType>> Find<TTimeoutMessageType>(Func<SagaTimeout<TTimeoutMessageType>, bool> predicate)
    {
        var messageType = MessageTypeName(typeof(TTimeoutMessageType));
        var found = new List<SagaTimeout<TTimeoutMessageType>>();

        foreach (var stored in storedTimeouts.Where(t => t.MessageType == messageType))
        {
            var timeout = new SagaTimeout<TTimeoutMessageType>(stored.TimeoutId, stored.DueTime, (TTimeoutMessageType)Deserialize(stored, typeof(TTimeoutMessageType)));
            if (!predicate(timeout))
            {
                continue;
            }

            // The typed instance is kept so a reschedule stores the payload the user may have changed.
            foundPayloads[stored.TimeoutId] = (timeout.Payload!, typeof(TTimeoutMessageType));
            found.Add(timeout);
        }

        return found;
    }

    public void Reschedule(string timeoutId, DateTimeOffset newDueTime)
    {
        var stored = storedTimeouts.Find(t => t.TimeoutId == timeoutId);
        if (stored is null)
        {
            changes.Add(new SagaTimeoutChange(SagaTimeoutChangeKind.RescheduleRejected, timeoutId, null, newDueTime));
            return;
        }

        Type payloadType;
        if (foundPayloads.TryGetValue(timeoutId, out var found))
        {
            stored.Payload = Serialize(found.Payload);
            stored.ContentType = serializer.ContentType;
            payloadType = found.PayloadType;
        }
        else
        {
            payloadType = (messageMetadataRegistry.GetMessageMetadata(stored.MessageType)
                ?? throw new InvalidOperationException($"Can't reschedule saga timeout '{timeoutId}' because the message type '{stored.MessageType}' isn't registered.")).MessageType;
        }

        stored.DueTime = newDueTime;
        stored.TriggerId = Guid.NewGuid().ToString();
        stored.Version++;
        pendingTimeouts[timeoutId] = new PendingSagaTimeout(stored, payloadType);
        operations.Add(new TimeoutOperation(TimeoutOperationKind.Reschedule, timeoutId, Snapshot(stored)));
        changes.Add(new SagaTimeoutChange(SagaTimeoutChangeKind.Rescheduled, timeoutId, Snapshot(stored), null));
    }

    // The payload is stored as received because operator requests are applied without deserializing it
    public StoredSagaTimeout RescheduleRequestedByOperator(StoredSagaTimeout stored, DateTimeOffset newDueTime, ReadOnlyMemory<byte> payload, string? contentType, Guid operationId)
    {
        if (!payload.IsEmpty)
        {
            stored.Payload = payload.ToArray();
            stored.ContentType = contentType ?? throw new ArgumentNullException(nameof(contentType));
        }

        stored.DueTime = newDueTime;
        stored.TriggerId = Guid.NewGuid().ToString();
        stored.Version++;
        _ = foundPayloads.Remove(stored.TimeoutId);
        operations.Add(new TimeoutOperation(TimeoutOperationKind.Reschedule, stored.TimeoutId, Snapshot(stored)));
        changes.Add(new SagaTimeoutChange(SagaTimeoutChangeKind.Rescheduled, stored.TimeoutId, Snapshot(stored), null, operationId));
        return stored;
    }

    public void Cancel(string timeoutId, Guid? operationId = null)
    {
        var stored = Remove(timeoutId, TimeoutOperationKind.Cancel);
        changes.Add(stored is null
            ? new SagaTimeoutChange(SagaTimeoutChangeKind.CancelRejected, timeoutId, null, null, operationId)
            : new SagaTimeoutChange(SagaTimeoutChangeKind.Canceled, timeoutId, Snapshot(stored), null, operationId));
    }

    public void Reject(SagaTimeoutChangeKind kind, string timeoutId, DateTimeOffset? requestedDueTime, string reason, Guid operationId) =>
        changes.Add(new SagaTimeoutChange(kind, timeoutId, null, requestedDueTime, operationId, reason));

    public void MarkFired(string timeoutId) => _ = Remove(timeoutId, TimeoutOperationKind.MarkFired);

    StoredSagaTimeout? Remove(string timeoutId, TimeoutOperationKind operationKind)
    {
        var stored = storedTimeouts.Find(t => t.TimeoutId == timeoutId);
        if (stored is not null)
        {
            _ = storedTimeouts.Remove(stored);
            operations.Add(new TimeoutOperation(operationKind, timeoutId, null));
        }

        _ = pendingTimeouts.Remove(timeoutId);
        _ = foundPayloads.Remove(timeoutId);
        return stored;
    }

    // Identifies the timeout and its scheduled instance, so a rescheduled timeout makes earlier messages stale.
    public static string Identity(StoredSagaTimeout stored) => $"{stored.TimeoutId}-{stored.TriggerId}";

    public StoredSagaTimeout? Get(string timeoutId) => storedTimeouts.Find(t => t.TimeoutId == timeoutId);

    public StoredSagaTimeout? GetByIdentity(string identity) => storedTimeouts.Find(t => Identity(t) == identity);

    public object Deserialize(StoredSagaTimeout stored, Type messageType)
    {
        return serializer.Deserialize(stored.Payload, stored.ContentType, messageType);
    }

    // Applied in the order the changes were made, after the saga itself is saved or updated
    public async Task Persist(ITimeoutPersister persister, IContainSagaData sagaData, ISynchronizedStorageSession session, ContextBag context, CancellationToken cancellationToken = default)
    {
        foreach (var operation in operations)
        {
            var task = operation.Kind switch
            {
                TimeoutOperationKind.Request => persister.Request(sagaData, operation.Timeout!, session, context, cancellationToken),
                TimeoutOperationKind.Reschedule => persister.Reschedule(sagaData, operation.Timeout!, session, context, cancellationToken),
                TimeoutOperationKind.Cancel => persister.Cancel(sagaData, operation.TimeoutId, session, context, cancellationToken),
                TimeoutOperationKind.MarkFired => persister.MarkFired(sagaData, operation.TimeoutId, session, context, cancellationToken),
                _ => throw new InvalidOperationException($"Unknown saga timeout operation '{operation.Kind}'.")
            };
            await task.ConfigureAwait(false);
        }

        operations.Clear();
    }

    public static string MessageTypeName(Type messageType) => messageType.FullName!;

    byte[] Serialize(object payload) => serializer.Serialize(payload);

    static StoredSagaTimeout Snapshot(StoredSagaTimeout stored) => new()
    {
        TimeoutId = stored.TimeoutId,
        TriggerId = stored.TriggerId,
        Version = stored.Version,
        DueTime = stored.DueTime,
        MessageType = stored.MessageType,
        ContentType = stored.ContentType,
        Payload = stored.Payload
    };

    readonly List<StoredSagaTimeout> storedTimeouts = [.. loadedTimeouts];
    readonly List<TimeoutOperation> operations = [];
    readonly List<SagaTimeoutChange> changes = [];
    readonly Dictionary<string, PendingSagaTimeout> pendingTimeouts = [];
    readonly Dictionary<string, (object Payload, Type PayloadType)> foundPayloads = [];
}

record PendingSagaTimeout(StoredSagaTimeout Stored, Type PayloadType);

enum TimeoutOperationKind
{
    Request,
    Reschedule,
    Cancel,
    MarkFired
}

// Timeout is a snapshot taken when the operation was recorded; it's null for Cancel and MarkFired.
sealed record TimeoutOperation(TimeoutOperationKind Kind, string TimeoutId, StoredSagaTimeout? Timeout);

enum SagaTimeoutChangeKind
{
    Requested,
    Rescheduled,
    Canceled,
    RescheduleRejected,
    CancelRejected
}

// Timeout is a snapshot taken when the change was made; it's null for rejected changes. OperationId is set for changes an operator requested.
sealed record SagaTimeoutChange(SagaTimeoutChangeKind Kind, string TimeoutId, StoredSagaTimeout? Timeout, DateTimeOffset? RequestedDueTime, Guid? OperationId = null, string RejectionReason = SagaTimeoutChangeRejectionReasons.TimeoutNotFound)
{
    public DateTimeOffset ChangedAt { get; } = DateTimeOffset.UtcNow;
}

static class SagaTimeoutChangeRejectionReasons
{
    // Fired and canceled timeouts are removed from storage, so the endpoint can't tell them apart from an unknown ID
    public const string TimeoutNotFound = "TimeoutNotFound";
    public const string VersionMismatch = "VersionMismatch";
    public const string SagaNotFound = "SagaNotFound";
}
