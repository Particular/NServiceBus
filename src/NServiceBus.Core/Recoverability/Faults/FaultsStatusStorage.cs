using System;

namespace NServiceBus.Recoverability.Faults
{
    using System.Collections.Concurrent;

    class FaultsStatusStorage
    {
        public void AddException(string messageId, Exception exception)
        {
            failedMessages.AddOrUpdate(messageId, exception, (m, e) => e);
        }

        public bool TryGetException(string messageId, out Exception exception)
        {
            return failedMessages.TryGetValue(messageId, out exception);
        }

        public void ClearExceptions(string messageId)
        {
            Exception exc;
            failedMessages.TryRemove(messageId, out exc);
        }

        ConcurrentDictionary<string, Exception> failedMessages = new ConcurrentDictionary<string, Exception>();
    }
}
