#nullable enable

namespace NServiceBus;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Sagas;
using Serialization;

class SagaTimeouts(List<StoredSagaTimeout> storedTimeouts, IMessageSerializer mainSerializer, MessageDeserializerResolver deserializerResolver)
{
    public IReadOnlyCollection<StoredSagaTimeout> PendingTriggers => pendingTriggers.Values;

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
            ContentType = mainSerializer.ContentType,
            Payload = Serialize(payload!)
        };

        storedTimeouts.Add(timeout);
        pendingTriggers[timeout.TimeoutId] = timeout;
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
            foundPayloads[stored.TimeoutId] = timeout.Payload!;
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

        if (foundPayloads.TryGetValue(timeoutId, out var payload))
        {
            stored.Payload = Serialize(payload);
            stored.ContentType = mainSerializer.ContentType;
        }

        stored.DueTime = newDueTime;
        stored.TriggerId = Guid.NewGuid().ToString();
        stored.Version++;
        pendingTriggers[timeoutId] = stored;
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

        _ = pendingTriggers.Remove(timeoutId);
        _ = foundPayloads.Remove(timeoutId);
        return stored;
    }

    public StoredSagaTimeout? Get(string timeoutId) => storedTimeouts.Find(t => t.TimeoutId == timeoutId);

    public object Deserialize(StoredSagaTimeout stored, Type messageType)
    {
        var serializer = deserializerResolver.Resolve(new Dictionary<string, string> { [Headers.ContentType] = stored.ContentType });
        return serializer.Deserialize(stored.Payload, [messageType])[0];
    }

    public static string MessageTypeName(Type messageType) => messageType.FullName!;

    byte[] Serialize(object payload)
    {
        using var stream = new MemoryStream();
        mainSerializer.Serialize(payload, stream);
        return stream.ToArray();
    }

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
    readonly Dictionary<string, StoredSagaTimeout> pendingTriggers = [];
    readonly Dictionary<string, object> foundPayloads = [];
}

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
