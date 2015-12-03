﻿namespace NServiceBus
{
    using System.Collections.Concurrent;

    class FlrStatusStorage
    {
        public void ClearFailuresForMessage(string messageId)
        {
            int e;
            failuresPerMessage.TryRemove(messageId, out e);
        }

        public void IncrementFailuresForMessage(string messageId)
        {
            failuresPerMessage.AddOrUpdate(messageId, 1, (s, i) => i + 1);
        }

        public int GetFailuresForMessage(string messageId)
        {
            int e;
            return !failuresPerMessage.TryGetValue(messageId, out e) ? 0 : e;
        }

        public void ClearAllFailures()
        {
            failuresPerMessage.Clear();
        }

        ConcurrentDictionary<string, int> failuresPerMessage = new ConcurrentDictionary<string, int>();
    }
}