namespace NServiceBus;

using System;
using System.Collections.Generic;

class InMemoryReceiveTransaction : IInMemoryReceiveTransaction
{
    readonly List<BrokerEnvelope> pendingEnvelopes = [];
    readonly object lockObj = new();
    bool committed;

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
            committed = true;
        }
    }

    public void Rollback()
    {
        lock (lockObj)
        {
            pendingEnvelopes.Clear();
            committed = false;
        }
    }
}
