namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using System.Transactions;
    using Extensibility;
    using Gateway.Deduplication;

    partial class InMemoryGatewayDeduplication : IDeduplicateMessages
    {
        public InMemoryGatewayDeduplication(ClientIdStorage clientIdStorage)
        {
            this.clientIdStorage = clientIdStorage;
        }

        public Task<bool> DeduplicateMessage(string clientId, DateTime timeReceived, ContextBag context)
        {
            if (clientIdStorage.IsDuplicate(clientId))
            {
                return TaskEx.FalseTask;
            }

            //the current design of the gateway seam will only allow us to safly add in transaction commit
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