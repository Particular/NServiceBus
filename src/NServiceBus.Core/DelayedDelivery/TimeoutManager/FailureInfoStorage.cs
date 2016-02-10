namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using JetBrains.Annotations;

    // The data structure has fixed maximum size. When the data structure reaches its maximum size,
    // the least recently used (LRU) message processing failure is removed from the storage.
    class FailureInfoStorage
    {
        public FailureInfoStorage(int maxElements = 1000)
        {
            this.maxElements = maxElements;
        }

        public void RecordFailureInfoForMessage(string messageId, Exception exception)
        {
            lock (lockObject)
            {
                FailureInfoNode node;
                if (failureInfoPerMessage.TryGetValue(messageId, out node))
                {
                    // We have seen this message before, just update the counter and store exception.
                    node.FailureInfo = new ProcessingFailureInfo(node.FailureInfo.NumberOfFailedAttempts + 1, exception);

                    // Maintain invariant: leastRecentlyUsedMessages.First contains the LRU item.
                    leastRecentlyUsedMessages.Remove(node.LeastRecentlyUsedEntry);
                    leastRecentlyUsedMessages.AddLast(node.LeastRecentlyUsedEntry);
                }
                else
                {
                    if (failureInfoPerMessage.Count == maxElements)
                    {
                        // We have reached the maximum allowed capacity. Remove the LRU item.
                        var leastRecentlyUsedEntry = leastRecentlyUsedMessages.First;
                        failureInfoPerMessage.Remove(leastRecentlyUsedEntry.Value);
                        leastRecentlyUsedMessages.RemoveFirst();
                    }

                    var newNode = new FailureInfoNode(
                        messageId,
                        new ProcessingFailureInfo(1, exception));

                    failureInfoPerMessage[messageId] = newNode;

                    // Maintain invariant: leastRecentlyUsedMessages.First contains the LRU item.
                    leastRecentlyUsedMessages.AddLast(newNode.LeastRecentlyUsedEntry);
                }
            }
        }

        [NotNull]
        public ProcessingFailureInfo GetFailureInfoForMessage(string messageId)
        {
            lock (lockObject)
            {
                FailureInfoNode node;
                if (failureInfoPerMessage.TryGetValue(messageId, out node))
                {
                    return node.FailureInfo;
                }
                return ProcessingFailureInfo.NullFailureInfo;
            }
        }

        public void ClearFailureInfoForMessage(string messageId)
        {
            lock (lockObject)
            {
                failureInfoPerMessage.Remove(messageId);
                leastRecentlyUsedMessages.Remove(messageId);
            }
        }

        Dictionary<string, FailureInfoNode> failureInfoPerMessage = new Dictionary<string, FailureInfoNode>();
        LinkedList<string> leastRecentlyUsedMessages = new LinkedList<string>();
        object lockObject = new object();

        int maxElements;

        class FailureInfoNode
        {
            public FailureInfoNode(string messageId, ProcessingFailureInfo failureInfo)
            {
                FailureInfo = failureInfo;
                LeastRecentlyUsedEntry = new LinkedListNode<string>(messageId);
            }

            public ProcessingFailureInfo FailureInfo { get; set; }
            public LinkedListNode<string> LeastRecentlyUsedEntry { get; }
        }
    }
}