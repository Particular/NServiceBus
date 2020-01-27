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
            if (clientIdSet.TryGetValue(clientId, out var existingNode)) // O(1)
            {
                clientIdList.Remove(existingNode); // O(1) operation, because we got the node reference
                clientIdList.AddLast(existingNode); // O(1) operation

                return true;
            }

            return false;
        }

        public void RegisterClientId(string clientId)
        {
            lock (clientIdSet)
            {
                //another thread might already have added the key
                if (clientIdSet.ContainsKey(clientId))
                {
                    //just return, we can't throw since that will lose the message if the receive operation isn't enlisted in the scope
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
