#nullable enable

namespace NServiceBus;

using System;
using System.Collections.Generic;
using System.Linq;
using Sagas;

class SagaTimeouts(List<StoredSagaTimeout> storedTimeouts, TimeoutSerializer serializer)
{
    public IReadOnlyCollection<StoredSagaTimeout> PendingTriggers => pendingTriggers.Values;

    public StoredSagaTimeout Add<TTimeoutMessageType>(TTimeoutMessageType payload, DateTimeOffset dueTime)
    {
        var timeout = new StoredSagaTimeout
        {
            TimeoutId = Guid.NewGuid().ToString(),
            TriggerId = Guid.NewGuid().ToString(),
            DueTime = dueTime,
            MessageType = MessageTypeName(typeof(TTimeoutMessageType)),
            ContentType = serializer.ContentType,
            Payload = Serialize(payload!)
        };

        storedTimeouts.Add(timeout);
        pendingTriggers[timeout.TimeoutId] = timeout;
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
            return;
        }

        if (foundPayloads.TryGetValue(timeoutId, out var payload))
        {
            stored.Payload = Serialize(payload);
            stored.ContentType = serializer.ContentType;
        }

        stored.DueTime = newDueTime;
        stored.TriggerId = Guid.NewGuid().ToString();
        pendingTriggers[timeoutId] = stored;
    }

    public void Cancel(string timeoutId)
    {
        _ = storedTimeouts.RemoveAll(t => t.TimeoutId == timeoutId);
        _ = pendingTriggers.Remove(timeoutId);
        _ = foundPayloads.Remove(timeoutId);
    }

    public StoredSagaTimeout? Get(string timeoutId) => storedTimeouts.Find(t => t.TimeoutId == timeoutId);

    public object Deserialize(StoredSagaTimeout stored, Type messageType)
    {
        return serializer.Deserialize(stored.Payload, stored.ContentType, messageType);
    }

    public static string MessageTypeName(Type messageType) => messageType.FullName!;

    byte[] Serialize(object payload) => serializer.Serialize(payload);

    readonly Dictionary<string, StoredSagaTimeout> pendingTriggers = [];
    readonly Dictionary<string, object> foundPayloads = [];
}
