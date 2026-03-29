namespace NServiceBus;

using System.Collections.Generic;
using System.Threading;
using Persistence.InMemory;

class InMemoryReceiveTransaction : IInMemoryReceiveTransaction
{
    readonly List<BrokerEnvelope> pendingEnvelopes = [];
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
                return pendingEnvelopes.ToArray();
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
            pendingEnvelopes.Clear();
            committed = false;
        }
    }
}
