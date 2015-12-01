namespace NServiceBus
{
    using System.Transactions;
    using NServiceBus.InMemory.Outbox;
    using NServiceBus.Outbox;
    using NServiceBus.Persistence;
    using NServiceBus.Transports;

    class InMemoryTransactionalSynchronizedStorageAdapter : ISynchronizedStorageAdapter
    {
        public bool TryAdapt(OutboxTransaction transaction, out CompletableSynchronizedStorageSession session)
        {
            var inMemOutboxTransaction = transaction as InMemoryOutboxTransaction;
            if (inMemOutboxTransaction != null)
            {
                session = new InMemorySynchronizedStorageSession(inMemOutboxTransaction.Transaction);
                return true;
            }
            session = null;
            return false;
        }

        public bool TryAdapt(TransportTransaction transportTransaction, out CompletableSynchronizedStorageSession session)
        {
            if (transportTransaction.Data.ContainsKey("AmbientTransaction"))
            {
                var ambientTransaction = transportTransaction.Data["AmbientTransaction"] as Transaction;
                if (ambientTransaction != null)
                {
                    var transaction = new InMemoryTransaction();
                    session = new InMemorySynchronizedStorageSession(transaction);
                    ambientTransaction.EnlistVolatile(new EnlistmentNotification(transaction), EnlistmentOptions.None);
                    return true;
                }
            }
            session = null;
            return false;
        }

        private class EnlistmentNotification : IEnlistmentNotification
        {
            InMemoryTransaction transaction;

            public EnlistmentNotification(InMemoryTransaction transaction)
            {
                this.transaction = transaction;
            }

            public void Prepare(PreparingEnlistment preparingEnlistment)
            {
                transaction.Commit();
                preparingEnlistment.Prepared();
            }

            public void Commit(Enlistment enlistment)
            {
                enlistment.Done();
            }

            public void Rollback(Enlistment enlistment)
            {
                enlistment.Done();
            }

            public void InDoubt(Enlistment enlistment)
            {
                enlistment.Done();
            }
        }
    }
}