namespace NServiceBus
{
    using System.Collections.Generic;

    class ClientIdStorage
    {
        public ClientIdStorage(int maxSize)
        {
            this.maxSize = maxSize;
        }

        public bool IsDuplicate(string clientId)
        {
            lock (clientIdSet)
            {
                // This implementation is not 100% since there is a race condition here where another thread might consider the same message as a non-duplicate.
                // We consider this good enough since this is the inmemory persister which will not be consistent when the endpoint is scaled out anyway.
                if (clientIdSet.TryGetValue(clientId, out var existingNode)) // O(1)
                {
                    clientIdList.Remove(existingNode); // O(1) operation, because we got the node reference
                    clientIdList.AddLast(existingNode); // O(1) operation

                    return true;
                }
            }

            return false;
        }

        public void RegisterClientId(string clientId)
        {
            lock (clientIdSet)
            {
                //another thread might already have added the ID since we checked the last time
                if (clientIdSet.ContainsKey(clientId))
                {
                    // another thread has proceed this ID already and there is a potential duplicate message but there is nothing we can do about it at this stage so just return.
                    // Throwing would just cause unessessary retries for the client.
                    return;
                }

                if (clientIdSet.Count == maxSize)
                {
                    var id = clientIdList.First.Value;
                    clientIdSet.Remove(id); // O(1)
                    clientIdList.RemoveFirst(); // O(1)
                }

                var node = clientIdList.AddLast(clientId); // O(1)
                clientIdSet.Add(clientId, node); // O(1)
            }
        }

        internal readonly int maxSize;
        readonly LinkedList<string> clientIdList = new LinkedList<string>();
        readonly Dictionary<string, LinkedListNode<string>> clientIdSet = new Dictionary<string, LinkedListNode<string>>();
    }
}
