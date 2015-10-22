namespace NServiceBus
{
    using System.Threading.Tasks;
    using System.Transactions;
    using NServiceBus.Extensibility;
    using NServiceBus.Features;
    using NServiceBus.InMemory.Outbox;
    using NServiceBus.Outbox;
    using NServiceBus.Persistence;
    using NServiceBus.Transports;

    class InMemoryTransactionalStorageFeature : Feature
    {
        /// <summary>
        ///     Called when the features is activated.
        /// </summary>
        protected internal override void Setup(FeatureConfigurationContext context)
        {
            context.Container.ConfigureComponent<InMemorySynchronizedStorage>(DependencyLifecycle.SingleInstance);
            context.Container.ConfigureComponent<InMemoryTransactionalSynchronizedStorageAdapter>(DependencyLifecycle.SingleInstance);
        }
    }

    class InMemorySynchronizedStorage : ISynchronizedStorage
    {
        public Task<CompletableSynchronizedStorageSession> OpenSession(ContextBag contextBag)
        {
            var session = (CompletableSynchronizedStorageSession)new InMemorySynchronizedStorageSession();
            return Task.FromResult(session);
        }
    }

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
            var ambientTransaction = transportTransaction as AmbientTransaction;
            if (ambientTransaction != null)
            {
                var transaction = new InMemoryTransaction();
                session = new InMemorySynchronizedStorageSession(transaction);
                ambientTransaction.Transaction.EnlistVolatile(new EnlistmentNotification(transaction), EnlistmentOptions.None);
                return true;
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