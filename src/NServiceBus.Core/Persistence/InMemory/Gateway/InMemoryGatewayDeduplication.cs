namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using System.Transactions;
    using Extensibility;
    using Gateway.Deduplication;

    class InMemoryGatewayDeduplication : IDeduplicateMessages
    {
        public InMemoryGatewayDeduplication(ClientIdStorage clientIdStorage)
        {
            this.clientIdStorage = clientIdStorage;
        }

        public Task<bool> DeduplicateMessage(string clientId, DateTime timeReceived, ContextBag context)
        {
            // since this storage is best effort given that scaling out will lead to duplicates anyway we decided to not
            // add any locking here. 2 threads could potentially treat the same ID as a non duplicate but that is unlikely to happen
            // since transports will most times only allow a single comsumer of a given message
            if (clientIdStorage.IsDuplicate(clientId))
            {
                return TaskEx.FalseTask;
            }

            // The current design of the gateway seam will only allow us to safly add the id when the scope commits.
            // This is fine since the gateway will always wrap the call in a transaction scope so this should always be true
            if (Transaction.Current != null)
            {
                Transaction.Current.EnlistVolatile(new EnlistmentNotification(clientIdStorage, clientId), EnlistmentOptions.None);
            }

            return TaskEx.TrueTask;
        }

        readonly ClientIdStorage clientIdStorage;

        class EnlistmentNotification : IEnlistmentNotification
        {
            public EnlistmentNotification(ClientIdStorage clientIdStorage, string clientId)
            {
                this.clientIdStorage = clientIdStorage;
                this.clientId = clientId;
            }

            public void Prepare(PreparingEnlistment preparingEnlistment)
            {
                try
                {
                    preparingEnlistment.Prepared();
                }
                catch (Exception ex)
                {
                    preparingEnlistment.ForceRollback(ex);
                }
            }

            public void Commit(Enlistment enlistment)
            {
                clientIdStorage.RegisterClientId(clientId);

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

            readonly ClientIdStorage clientIdStorage;
            readonly string clientId;
        }
    }
}