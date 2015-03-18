namespace NServiceBus.FirstLevelRetries
{
    using System;
    using System.Collections.Concurrent;

    class FlrStatusStorage
    {
        public void ClearFailuresForMessage(string messageId)
        {
            int e;
            failuresPerMessage.TryRemove(messageId, out e);
        }

        public void IncrementFailuresForMessage(string messageId, Exception e)
        {
            failuresPerMessage.AddOrUpdate(messageId,1,
                (s, i) => i + 1);
        }

        public int GetRetriesForMessage(string messageId)
        {
            int e;

            if (!failuresPerMessage.TryGetValue(messageId, out e))
            {
                return 0;
            }
            return e;
        }

        ConcurrentDictionary<string, int> failuresPerMessage = new ConcurrentDictionary<string, int>();
    }
}