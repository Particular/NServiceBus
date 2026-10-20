#nullable enable

namespace NServiceBus;

using System;
using System.Collections.Generic;
using System.Linq;
using Sagas;
using Unicast.Messages;

class SagaTimeouts(List<StoredSagaTimeout> storedTimeouts, TimeoutSerializer serializer, MessageMetadataRegistry messageMetadataRegistry)
{
    public const string TimeoutIdHeader = "NServiceBus.SagaTimeoutId";

    public IReadOnlyCollection<PendingSagaTimeout> PendingTimeouts => pendingTimeouts.Values;

    public IReadOnlyList<SagaTimeoutChange> Changes => changes;

    public bool HasStoredTimeouts => storedTimeouts.Count > 0;

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
        changes.Add(new SagaTimeoutChange(SagaTimeoutChangeKind.Requested, timeout.TimeoutId, Snapshot(timeout), null));
        return timeout;
    }

    public IReadOnlyList<SagaTimeout<TTimeoutMessageType>> Find<TTimeoutMessageType>(Func<SagaTimeout<TTimeoutMessageType>, bool> predicate)
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
        changes.Add(new SagaTimeoutChange(SagaTimeoutChangeKind.Rescheduled, timeoutId, Snapshot(stored), null));
    }

    public void Cancel(string timeoutId)
    {
        var stored = Remove(timeoutId);
        changes.Add(stored is null
            ? new SagaTimeoutChange(SagaTimeoutChangeKind.CancelRejected, timeoutId, null, null)
            : new SagaTimeoutChange(SagaTimeoutChangeKind.Canceled, timeoutId, Snapshot(stored), null));
    }

    public StoredSagaTimeout? Remove(string timeoutId)
    {
        var stored = storedTimeouts.Find(t => t.TimeoutId == timeoutId);
        if (stored is not null)
        {
            _ = storedTimeouts.Remove(stored);
        }

        _ = pendingTimeouts.Remove(timeoutId);
        _ = foundPayloads.Remove(timeoutId);
        return stored;
    }

    // Identifies the timeout and its scheduled instance, so a rescheduled timeout makes earlier messages stale.
    public static string Identity(StoredSagaTimeout stored) => $"{stored.TimeoutId}-{stored.TriggerId}";

    public StoredSagaTimeout? GetByIdentity(string identity) => storedTimeouts.Find(t => Identity(t) == identity);

    public object Deserialize(StoredSagaTimeout stored, Type messageType)
    {
        return serializer.Deserialize(stored.Payload, stored.ContentType, messageType);
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

    readonly List<SagaTimeoutChange> changes = [];
    readonly Dictionary<string, PendingSagaTimeout> pendingTimeouts = [];
    readonly Dictionary<string, (object Payload, Type PayloadType)> foundPayloads = [];
}

record PendingSagaTimeout(StoredSagaTimeout Stored, Type PayloadType);

enum SagaTimeoutChangeKind
{
    Requested,
    Rescheduled,
    Canceled,
    RescheduleRejected,
    CancelRejected
}

// Timeout is a snapshot taken when the change was made; it's null for rejected changes
sealed record SagaTimeoutChange(SagaTimeoutChangeKind Kind, string TimeoutId, StoredSagaTimeout? Timeout, DateTimeOffset? RequestedDueTime)
{
    public DateTimeOffset ChangedAt { get; } = DateTimeOffset.UtcNow;
}
