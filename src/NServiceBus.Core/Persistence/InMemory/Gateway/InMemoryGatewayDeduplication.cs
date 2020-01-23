namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using System.Transactions;
    using Extensibility;
    using Gateway.Deduplication;

    class InMemoryGatewayDeduplication : IDeduplicateMessages
    {
        public InMemoryGatewayDeduplication(int maxSize)
        {
            this.maxSize = maxSize;
        }

        public Task<bool> DeduplicateMessage(string clientId, DateTime timeReceived, ContextBag context)
        {
            lock (clientIdSet)
            {
                // Return FALSE if item EXISTS, TRUE if ADDED
                if (clientIdSet.TryGetValue(clientId, out var existingNode)) // O(1)
                {
                    clientIdList.Remove(existingNode); // O(1) operation, because we got the node reference
                    clientIdList.AddLast(existingNode); // O(1) operation

                    return TaskEx.FalseTask;
                }
                else
                {
                    if (clientIdSet.Count == maxSize)
                    {
                        var id = clientIdList.First.Value;
                        clientIdSet.Remove(id); // O(1)
                        clientIdList.RemoveFirst(); // O(1)
                    }

                    var node = clientIdList.AddLast(clientId); // O(1)
                    clientIdSet.Add(clientId, node); // O(1)

                    if (Transaction.Current != null)
                    {
                        Transaction.Current.EnlistVolatile(new EnlistmentNotification(clientIdSet, clientId), EnlistmentOptions.None);
                    }

                    return TaskEx.TrueTask;
                }
            }
        }

        internal readonly int maxSize;
        readonly LinkedList<string> clientIdList = new LinkedList<string>();
        readonly Dictionary<string, LinkedListNode<string>> clientIdSet = new Dictionary<string, LinkedListNode<string>>();

        class EnlistmentNotification : IEnlistmentNotification
        {
            public EnlistmentNotification(Dictionary<string, LinkedListNode<string>> clientIdSet, string clientId)
            {
                this.clientIdSet = clientIdSet;
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
                enlistment.Done();
            }

            public void Rollback(Enlistment enlistment)
            {
                lock (clientIdSet)
                {
                    clientIdSet.Remove(clientId);
                }
                enlistment.Done();
            }

            public void InDoubt(Enlistment enlistment)
            {
                enlistment.Done();
            }

            readonly Dictionary<string, LinkedListNode<string>> clientIdSet;
            readonly string clientId;
        }
    }
}