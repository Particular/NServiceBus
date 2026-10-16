#nullable enable

namespace NServiceBus;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Sagas;
using Serialization;
using Unicast.Messages;

class SagaTimeouts(List<StoredSagaTimeout> storedTimeouts, IMessageSerializer mainSerializer, MessageDeserializerResolver deserializerResolver, MessageMetadataRegistry messageMetadataRegistry)
{
    public const string TimeoutIdHeader = "NServiceBus.SagaTimeoutId";

    public IReadOnlyCollection<PendingSagaTimeout> PendingTimeouts => pendingTimeouts.Values;

    public StoredSagaTimeout Add<TTimeoutMessageType>(TTimeoutMessageType payload, DateTimeOffset dueTime)
    {
        var timeout = new StoredSagaTimeout
        {
            TimeoutId = Guid.NewGuid().ToString(),
            TriggerId = Guid.NewGuid().ToString(),
            DueTime = dueTime,
            MessageType = MessageTypeName(typeof(TTimeoutMessageType)),
            ContentType = mainSerializer.ContentType,
            Payload = Serialize(payload!)
        };

        storedTimeouts.Add(timeout);
        pendingTimeouts[timeout.TimeoutId] = new PendingSagaTimeout(timeout, typeof(TTimeoutMessageType));
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
            return;
        }

        Type payloadType;
        if (foundPayloads.TryGetValue(timeoutId, out var found))
        {
            stored.Payload = Serialize(found.Payload);
            stored.ContentType = mainSerializer.ContentType;
            payloadType = found.PayloadType;
        }
        else
        {
            payloadType = (messageMetadataRegistry.GetMessageMetadata(stored.MessageType)
                ?? throw new InvalidOperationException($"Can't reschedule saga timeout '{timeoutId}' because the message type '{stored.MessageType}' isn't registered.")).MessageType;
        }

        stored.DueTime = newDueTime;
        stored.TriggerId = Guid.NewGuid().ToString();
        pendingTimeouts[timeoutId] = new PendingSagaTimeout(stored, payloadType);
    }

    public void Cancel(string timeoutId)
    {
        _ = storedTimeouts.RemoveAll(t => t.TimeoutId == timeoutId);
        _ = pendingTimeouts.Remove(timeoutId);
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

    readonly Dictionary<string, PendingSagaTimeout> pendingTimeouts = [];
    readonly Dictionary<string, (object Payload, Type PayloadType)> foundPayloads = [];
}

record PendingSagaTimeout(StoredSagaTimeout Stored, Type PayloadType);
