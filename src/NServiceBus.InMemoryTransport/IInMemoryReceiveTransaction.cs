namespace NServiceBus;

using System.Collections.Generic;

interface IInMemoryReceiveTransaction
{
    void Enlist(BrokerEnvelope envelope);
    IReadOnlyList<BrokerEnvelope> GetPendingAndClear();
    void Commit();
    void Rollback();
}
