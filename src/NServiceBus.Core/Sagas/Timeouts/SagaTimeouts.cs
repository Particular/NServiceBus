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
            stored.ContentType = mainSerializer.ContentType;
        }

        stored.DueTime = newDueTime;
        stored.TriggerId = Guid.NewGuid().ToString();
        stored.Version++;
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

    readonly Dictionary<string, StoredSagaTimeout> pendingTriggers = [];
    readonly Dictionary<string, object> foundPayloads = [];
}
