namespace NServiceBus;

using System.Collections.Generic;
using System.Threading;
using Persistence.InMemory;

class InMemoryReceiveTransaction : IInMemoryReceiveTransaction
{
    List<BrokerEnvelope> pendingEnvelopes = [];
    readonly Lock lockObj = new();
    bool committed;

    public InMemoryStorageTransaction StorageTransaction { get; } = new();

    public void Enlist(BrokerEnvelope envelope)
    {
        lock (lockObj)
        {
            pendingEnvelopes.Add(envelope);
        }
    }

    public IReadOnlyList<BrokerEnvelope> GetPendingAndClear()
    {
        lock (lockObj)
        {
            if (committed)
            {
                var result = pendingEnvelopes;
                pendingEnvelopes = [];
                return result;
            }
            pendingEnvelopes.Clear();
            return [];
        }
    }

    public void Commit()
    {
        lock (lockObj)
        {
            StorageTransaction.Commit();
            committed = true;
        }
    }

    public void Rollback()
    {
        lock (lockObj)
        {
            StorageTransaction.Rollback();
            foreach (var envelope in pendingEnvelopes)
            {
                envelope.Dispose();
            }
            pendingEnvelopes.Clear();
            committed = false;
        }
    }

    internal IReadOnlyList<BrokerEnvelope> GetPendingEnvelopesForTesting()
    {
        lock (lockObj)
        {
            return [.. pendingEnvelopes];
        }
    }
}
